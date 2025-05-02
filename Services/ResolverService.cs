// File: Services/Interfaces/IResolverService.cs
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DnsProxy.Data;
using DnsProxy.Models;

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
    /// <inheritdoc/>
    public class ResolverService(IHttpClientFactory httpFactory,
                           ILogger<ResolverService> log, IConfigService config) : IResolverService
    {
        private readonly IHttpClientFactory _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
        private readonly ILogger<ResolverService> _log = log ?? throw new ArgumentNullException(nameof(log));

        private static readonly Random _rnd = new();

        public async Task<(IPAddress? ip, int ttl, string upstream, string diag)> ResolveAsync(
            string domain, List<DnsServerEntry> pool)
        {
            if (pool.Count == 0)
                return (null, 0, "-", "Empty pool");
            var conf = await config.GetConfigAsync();
            var strategy = conf?.Strategy ?? ResolveStrategy.FirstSuccess;

            return strategy switch
            {
                ResolveStrategy.FirstSuccess => await ResolveSequentially(domain, pool),
                ResolveStrategy.ParallelAll => await ResolveInParallel(domain, pool),
                _ => throw new NotSupportedException($"Strategy {strategy} not supported"),
            };
        }
        private async Task<(IPAddress?, int, string, string)> ResolveSequentially(string domain, List<DnsServerEntry> pool)
        {
            Exception? lastEx = null;

            foreach (var s in pool.OrderBy(p => p.Priority))
            {
                try
                {
                    var result = await ResolveOne(domain, s);
                    if (result.ip != null)
                        return (result.ip, result.ttl, s.Address!, "NOERROR");

                    _log.LogWarning("[{addr}] returned NXDOMAIN for {domain}", s.Address, domain);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    _log.LogWarning(ex, "Upstream {addr} failed", s.Address);
                }
            }

            return lastEx != null
                ? (null, 0, pool.Last().Address!, lastEx.GetBaseException().Message)
                : (null, 0, pool.Last().Address!, "NXDOMAIN");
        }
        private async Task<(IPAddress?, int, string, string)> ResolveInParallel(string domain, List<DnsServerEntry> pool)
        {
            var dohTasks = pool
                .Where(s => s.Protocol != DnsProtocol.Udp)
                .Select(server =>
                    Task.Run(async () =>
                    {
                        try
                        {
                            var result = await ResolveOne(domain, server);
                            return (result.ip, result.ttl, server.Address!, result.ip != null ? "NOERROR" : "NXDOMAIN");
                        }
                        catch (Exception ex)
                        {
                            return (null, 0, server.Address!, ex.GetBaseException().Message);
                        }
                    })
                ).ToList();

            // Сначала ждём DoH-потоки
            while (dohTasks.Count > 0)
            {
                var finished = await Task.WhenAny(dohTasks);
                dohTasks.Remove(finished);

                var (ip, ttl, upstream, diag) = await finished;
                if (ip != null)
                    return (ip, ttl, upstream, diag);
            }

            // Если DoH не дали ответ — пробуем UDP
            var udpTasks = pool
                .Where(s => s.Protocol == DnsProtocol.Udp)
                .Select(server =>
                    Task.Run(async () =>
                    {
                        try
                        {
                            var result = await ResolveOne(domain, server);
                            return (result.ip, result.ttl, server.Address!, result.ip != null ? "NOERROR" : "NXDOMAIN");
                        }
                        catch (Exception ex)
                        {
                            return (null, 0, server.Address!, ex.GetBaseException().Message);
                        }
                    })
                ).ToList();

            while (udpTasks.Count > 0)
            {
                var finished = await Task.WhenAny(udpTasks);
                udpTasks.Remove(finished);

                var (ip, ttl, upstream, diag) = await finished;
                if (ip != null)
                    return (ip, ttl, upstream, diag);
            }

            // Вообще ничего не сработало
            return (null, 0, pool.Last().Address!, "NXDOMAIN");
        }
        private async Task<(IPAddress? ip, int ttl)> ResolveOne(string domain, DnsServerEntry s)
        {
            return s.Protocol switch
            {
                DnsProtocol.Udp => await QueryUdpAsync(domain, s),
                DnsProtocol.DoH_Wire => await QueryDoHWireManualAsync(domain, s),
                DnsProtocol.DoH_Json => await QueryDoHJsonAsync(domain, s),
                _ => throw new NotSupportedException($"Protocol {s.Protocol} not supported")
            };
        }
        private async Task<(IPAddress?, int)> QueryUdpAsync(string domain, DnsServerEntry s)
        {
            // стандартный DNS по UDP через DnsClient
            var client = new DnsClient.LookupClient(
                new DnsClient.LookupClientOptions(
                    new DnsClient.NameServer(IPAddress.Parse(s.Address)))
                {
                    Timeout = TimeSpan.FromSeconds(2),
                    UseTcpFallback = true
                });
            var res = await client.QueryAsync(domain, DnsClient.QueryType.A);
            var rec = res.Answers.ARecords().FirstOrDefault();
            return rec == null
                ? (null, 0)
                : (rec.Address, (int)rec.TimeToLive);
        }

        private async Task<(IPAddress?, int)> QueryDoHWireManualAsync(string domain, DnsServerEntry s)
        {
            // 1. нормализуем URL
            var baseAddr = s.Address.TrimEnd('/');
            if (!baseAddr.Contains('/'))
                baseAddr += "/dns-query";

            // 2. собираем raw DNS-запрос вручную
            byte[] wire = BuildWireQuery(domain);

            // 3. base64url
            string b64 = Convert.ToBase64String(wire)
                             .TrimEnd('=')
                             .Replace('+', '-')
                             .Replace('/', '_');
            var url = $"{baseAddr}?dns={b64}";

            // 4. шлём GET HTTP/2 + Accept
            var http = _httpFactory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/dns-message"));

            var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _log.LogWarning("[DoH-Wire] {url} => {code} / {body}",
                                url, resp.StatusCode, body);
                return (null, 0);
            }

            var data = await resp.Content.ReadAsByteArrayAsync();
            // 5. разбираем ответ вручную
            return ParseAFromWire(data);
        }

        private async Task<(IPAddress?, int)> QueryDoHJsonAsync(string domain, DnsServerEntry s)
        {
            // 1. нормализуем URL
            var baseAddr = s.Address.TrimEnd('/');
            if (!baseAddr.Contains('/'))
                baseAddr += "/resolve";

            // 2. собираем QueryString
            var ub = new UriBuilder(baseAddr);
            var qs = System.Web.HttpUtility.ParseQueryString(ub.Query);
            qs["name"] = domain;
            qs["type"] = "A";
            ub.Query = qs.ToString();

            var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

            var resp = await http.GetAsync(ub.Uri);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _log.LogWarning("[DoH-Json] {url} => {code} / {body}",
                                ub.Uri, resp.StatusCode, body);
                return (null, 0);
            }

            using var doc = await JsonSerializer.DeserializeAsync<JsonDocument>(
                                await resp.Content.ReadAsStreamAsync());
            if (doc == null || !doc.RootElement.TryGetProperty("Answer", out var answers))
                return (null, 0);

            foreach (var el in answers.EnumerateArray())
            {
                if (el.GetProperty("type").GetInt32() != 1) continue;
                var ip = IPAddress.Parse(el.GetProperty("data").GetString()!);
                var ttl = el.GetProperty("TTL").GetInt32();
                return (ip, ttl);
            }
            return (null, 0);
        }
        private static byte[] BuildWireQuery(string domain)
        {
            // 12-byte DNS header
            ushort id = (ushort)_rnd.Next(0, 0x10000);
            var header = new byte[]
            {
                (byte)(id >> 8), (byte)id,
                0x01, 0x00,   // RD=1
                0x00, 0x01,   // QDCOUNT=1
                0x00, 0x00,   // ANCOUNT
                0x00, 0x00,   // NSCOUNT
                0x00, 0x00    // ARCOUNT
            };

            // QNAME
            var q = new List<byte>();
            foreach (var lbl in domain.Split('.'))
            {
                var bs = Encoding.ASCII.GetBytes(lbl);
                q.Add((byte)bs.Length);
                q.AddRange(bs);
            }
            q.Add(0x00);            // term label
            q.Add(0x00); q.Add(0x01);// QTYPE=A
            q.Add(0x00); q.Add(0x01);// QCLASS=IN

            return header.Concat(q).ToArray();
        }

        private static (IPAddress?, int) ParseAFromWire(byte[] data)
        {
            // простой разбор: пропускаем заголовок+вопрос, ищем первую запись типа A (0x0001)
            int pos = 12;
            // пропускаем QNAME
            while (data[pos] != 0) pos += data[pos] + 1;
            pos += 5; // null + QTYPE(2) + QCLASS(2)

            // ANCOUNT
            int ancount = (data[6] << 8) | data[7];
            for (int i = 0; i < ancount; i++)
            {
                // NAME (2 bytes, может быть pointer 0xC0..)
                pos += 2;
                // TYPE
                ushort type = (ushort)((data[pos] << 8) | data[pos + 1]);
                pos += 2;
                // CLASS
                pos += 2;
                // TTL
                int ttl = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
                pos += 4;
                // RDLENGTH
                int len = (data[pos] << 8) | data[pos + 1];
                pos += 2;
                if (type == 1 && len == 4)
                {
                    var ip = new IPAddress(data.Skip(pos).Take(4).ToArray());
                    return (ip, ttl);
                }
                pos += len;
            }
            return (null, 0);
        }
    }
}