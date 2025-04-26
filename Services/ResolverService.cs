// File: Services/Interfaces/IResolverService.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ARSoft.Tools.Net.Dns;
using DnsClient;
using DnsProxy.Models;
using DnsProxy.Services;
using Microsoft.Extensions.Caching.Memory;

namespace DnsProxy.Services
{
    /// <summary>
    /// Выполняет разрешение доменных имён через пул DNS-серверов.
    /// </summary>
    public interface IResolverService
    {
        /// <summary>
        /// Пытается разрешить указанный домен через список серверов.
        /// Возвращает кортеж: (IP, TTL, адрес upstream, diagnostic message).
        /// </summary>
        Task<(IPAddress? ip, int ttl, string upstream, string diag)> ResolveAsync(
            string domain,
            List<DnsServerEntry> pool
        );
    }
}

namespace DnsProxy.Services
{
    public sealed class ResolverService : IResolverService
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ResolverService> _log;
        private readonly IMemoryCache _cache;

        public ResolverService(IHttpClientFactory http,
                               ILogger<ResolverService> log,
                               IMemoryCache cache)
        {
            _http = http;
            _log = log;
            _cache = cache;
        }

        /* ───────── PUBLIC ───────── */

        public async Task<(IPAddress? ip, int ttl, string upstream, string diag)>
            ResolveAsync(string domain, List<DnsServerEntry> pool)
        {
            Exception? last = null;

            foreach (var s in pool.OrderBy(p => p.Priority))
            {
                try
                {
                    var (ip, ttl) = s.Protocol switch
                    {
                        DnsProtocol.Udp => await QueryUdpAsync(domain, s),
                        DnsProtocol.DoH_Wire => await QueryWireAsync(domain, s),
                        DnsProtocol.DoH_Json => await QueryJsonAsync(domain, s),
                        _ => (null, 0)
                    };

                    if (ip != null)
                        return (ip, ttl, s.Address, "NOERROR");

                    _log.LogWarning("[{u}] NXDOMAIN for {d}", s.Address, domain);
                }
                catch (Exception ex)
                {
                    last = ex;
                    _log.LogWarning(ex, "Upstream {u} failed", s.Address);
                }
            }

            return (null, 0,
                    pool.LastOrDefault()?.Address ?? "-",
                    last?.GetBaseException().Message ?? "NXDOMAIN");
        }

        /* ───────── UDP ───────── */

        private static async Task<(IPAddress?, int)> QueryUdpAsync(string dom, DnsServerEntry s)
        {
            var lc = new LookupClient(
                        new LookupClientOptions(new NameServer(IPAddress.Parse(s.Address)))
                        {
                            Timeout = TimeSpan.FromSeconds(3),
                            UseTcpFallback = true
                        });

            var r = await lc.QueryAsync(dom, QueryType.A);
            var a = r.Answers.ARecords().FirstOrDefault();
            return a is null ? (null, 0) : (a.Address, (int)a.TimeToLive);
        }

        /* ───────── DoH-wire (RFC 8484) ───────── */

        private async Task<(IPAddress?, int)> QueryWireAsync(string dom, DnsServerEntry s)
        {
            var uri = new Uri(s.Address.TrimEnd('/'));           // https://dns.comss.one/dns-query
            var host = uri.Host;

            // 1. bootstrap-IP (кэшируется до 30 мин)
            var ipHost = s.StaticAddress ?? await BootstrapIpAsync(host);

            // 2. HTTP-клиент: соединяемся по IP, но URL сохраняем с host
            var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (ctx, ct) =>
                {
                    var sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await sock.ConnectAsync(ipHost, 443, ct);
                    return new NetworkStream(sock, ownsSocket: true);
                },
                SslOptions = { TargetHost = host }  // SNI
            };

            using var http = new HttpClient(handler);
            http.DefaultRequestHeaders.Accept.ParseAdd("application/dns-message");

            // 3. raw DNS-запрос
            byte[] raw = BuildWireQuery(dom);

            // Comss принимает ТОЛЬКО GET ?dns=  (POST выдаёт 400)
            var b64 = Convert.ToBase64String(raw)
                             .TrimEnd('=')
                             .Replace('+', '-')
                             .Replace('/', '_');

            var getUri = new UriBuilder(uri) { Query = $"dns={b64}" }.Uri;
            var resp = await http.GetAsync(getUri);

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("DoH-Wire {u} → {c}", getUri, resp.StatusCode);
                return (null, 0);
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var msg = DnsMessage.Parse(bytes);
            var aRec = msg.AnswerRecords.OfType<ARecord>().FirstOrDefault();

            return aRec == null ? (null, 0) : (aRec.Address, (int)aRec.TimeToLive);
        }

        /* ───────── DoH-JSON ───────── */

        private async Task<(IPAddress?, int)> QueryJsonAsync(string dom, DnsServerEntry s)
        {
            string baseUrl = s.Address.TrimEnd('/');
            if (!baseUrl.Contains('/')) baseUrl += "/dns-query";

            var ub = new UriBuilder(baseUrl);
            var qs = System.Web.HttpUtility.ParseQueryString(ub.Query);
            qs["name"] = dom;
            qs["type"] = "A";
            ub.Query = qs.ToString();

            using var http = _http.CreateClient();
            http.DefaultRequestHeaders.Accept.ParseAdd("application/dns-json");

            var resp = await http.GetAsync(ub.Uri);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("DoH-Json {u} → {c}", ub.Uri, resp.StatusCode);
                return (null, 0);
            }

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            if (!doc.RootElement.TryGetProperty("Answer", out var arr))
                return (null, 0);

            foreach (var el in arr.EnumerateArray())
                if (el.GetProperty("type").GetInt32() == 1)
                    return (IPAddress.Parse(el.GetProperty("data").GetString()!),
                            el.GetProperty("TTL").GetInt32());

            return (null, 0);
        }

        /* ───────── helpers ───────── */

        private static byte[] BuildWireQuery(string domain)
        {
            ushort id = (ushort)Random.Shared.Next(ushort.MaxValue);     // ID
            var buf = new List<byte>
        {
            (byte)(id >> 8), (byte)id,
            0x01, 0x00,   // RD=1
            0x00, 0x01,   // QDCOUNT
            0x00, 0x00,   // ANCOUNT
            0x00, 0x00,   // NSCOUNT
            0x00, 0x00    // ARCOUNT
        };

            foreach (var label in domain.Split('.'))
            {
                buf.Add((byte)label.Length);
                buf.AddRange(Encoding.ASCII.GetBytes(label));
            }
            buf.Add(0x00);              // end-label
            buf.Add(0x00); buf.Add(0x01); // QTYPE = A
            buf.Add(0x00); buf.Add(0x01); // QCLASS = IN
            return [.. buf];
        }

        private async Task<IPAddress> BootstrapIpAsync(string host)
        {
            if (_cache.TryGetValue(host, out IPAddress ip))
                return ip;

            var lookup = new LookupClient(NameServer.GooglePublicDns, NameServer.Cloudflare);
            var res = await lookup.QueryAsync(host, QueryType.A);
            var a = res.Answers.ARecords().FirstOrDefault()
                     ?? throw new InvalidOperationException($"bootstrap {host}");

            _cache.Set(host, a.Address,
                       TimeSpan.FromSeconds(Math.Min(a.TimeToLive, 1800)));
            return a.Address;
        }
    }
}
