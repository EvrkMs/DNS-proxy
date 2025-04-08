using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Text.Json;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using DNS_proxy.Core.Models;
using DNS_proxy.Data;
using Timer = System.Timers.Timer;

namespace DNS_proxy.Service;

public class CustomDnsServer : IDisposable
{
    private DnsServer? _dnsServer;

    #region Constants & Fields

    private readonly HttpClient _httpClient = new(
        new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            },
            EnableMultipleHttp2Connections = false
        })
    {
        DefaultRequestVersion = HttpVersion.Version20,
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };
    private readonly ConcurrentDictionary<string, CacheEntry> _positiveCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _comssNegativeCache = new();

    private static List<DnsRule> _rulesCache = new();
    private static DateTime _lastRulesLoaded = DateTime.MinValue;

    private readonly Timer _cleanupTimer;

    private const string comssDohUrl = "https://dns.comss.one/dns-query?name={0}&type=A";
    private const string cloudflareDohUrl = "https://cloudflare-dns.com/dns-query?name={0}&type=A";

    #endregion

    #region Constructor & Startup

    public CustomDnsServer()
    {
        ReloadRules(); // <---- ДОБАВЬ ЭТО
        _cleanupTimer = new Timer(30_000);
        _cleanupTimer.Elapsed += (_, _) => CleanupAndMaybeReloadRules();
        _cleanupTimer.Start();
    }
    public static void ReloadRulesPublic() => ReloadRules();

    public void Start()
    {
        _dnsServer = new DnsServer(53);
        _dnsServer.QueryReceived += OnQueryReceived;
        _dnsServer.Start();
        Log("DNS-сервер запущен на порту 53.");
    }

    public void Dispose()
    {
        Log("Завершение работы DNS-сервера...");
        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();
        _dnsServer?.Stop();
    }
    private void Log(string msg)
    {
        Console.WriteLine(msg);
        OnLog?.Invoke(msg);
    }

    public event Action<string>? OnLog;
    public void Stop()
    {
        _dnsServer?.Stop();
        Console.WriteLine("DNS-сервер остановлен.");
    }
    #endregion

    #region DNS Query Handling

    private async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage request)
            return;

        var clientIp = e.RemoteEndpoint?.Address?.MapToIPv4().ToString() ?? "0.0.0.0";
        DnsMessage response = request.CreateResponseInstance();

        foreach (var question in request.Questions.Where(q => q.RecordType == RecordType.A))
        {
            string domain = question.Name.ToString().TrimEnd('.');
            Console.WriteLine($"DNS-запрос от {clientIp} => {domain}");

            var decision = CheckRules(clientIp, domain);

            if (decision.IsBlocked)
            {
                Console.WriteLine("   -> BLOCK (NXDOMAIN)");
                response.ReturnCode = ReturnCode.NxDomain;
                continue;
            }

            if (decision.IsRewrite && IPAddress.TryParse(decision.RewriteIp, out var ip))
            {
                Console.WriteLine($"   -> REWRITE => {ip}");
                response.AnswerRecords.Add(new ARecord(question.Name, 60, ip));
                continue;
            }

            var resolvedIp = await ResolveDomainAsync(domain);
            if (resolvedIp != null)
            {
                Console.WriteLine($"   -> REAL => {resolvedIp}");
                response.AnswerRecords.Add(new ARecord(question.Name, 60, resolvedIp));
            }
            else
            {
                Console.WriteLine("   -> NXDOMAIN (не найден)");
                response.ReturnCode = ReturnCode.NxDomain;
            }
        }

        e.Response = response;
    }

    #endregion

    #region DNS Resolution (DoH / UDP)
    private static byte[] BuildDnsQuery(string domain)
    {
        var rand = new Random();
        ushort transactionId = (ushort)rand.Next(ushort.MaxValue);

        List<byte> message = new();

        // Transaction ID
        message.Add((byte)(transactionId >> 8));
        message.Add((byte)(transactionId & 0xFF));

        // Flags: standard query
        message.Add(0x01); // recursion desired
        message.Add(0x00);

        // QDCOUNT (1)
        message.Add(0x00);
        message.Add(0x01);

        // ANCOUNT, NSCOUNT, ARCOUNT = 0
        message.AddRange(new byte[6]);

        // QNAME
        var labels = domain.Split('.');
        foreach (var label in labels)
        {
            byte len = (byte)label.Length;
            message.Add(len);
            message.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
        }
        message.Add(0x00); // End of QNAME

        // QTYPE (A)
        message.Add(0x00);
        message.Add(0x01);

        // QCLASS (IN)
        message.Add(0x00);
        message.Add(0x01);

        return message.ToArray();
    }

    private async Task<IPAddress> ResolveDomainAsync(string domain)
    {
        // 🔐 Если домен — dns.comss.one → возвращаем статичный IP
        if (domain.Equals("dns.comss.one", StringComparison.OrdinalIgnoreCase))
        {
            var staticComssIp = IPAddress.Parse("83.220.169.155");
            Console.WriteLine($"   -> STATIC RESOLVE {domain} => {staticComssIp}");
            return staticComssIp;
        }

        // 1) Позитивный кэш
        if (_positiveCache.TryGetValue(domain, out var entry) && entry.Deadline > DateTime.UtcNow)
            return entry.IP;

        _positiveCache.TryRemove(domain, out _);

        // 2) Негативный кэш Comss
        bool skipComss = _comssNegativeCache.TryGetValue(domain, out var negDeadline) && negDeadline > DateTime.UtcNow;

        // 3) Comss
        if (!skipComss)
        {
            var ip = await ResolveViaDohWireFormat(domain, "dns.comss.one");
            if (ip != null)
            {
                _positiveCache[domain] = new(ip, DateTime.UtcNow.AddMinutes(5));
                return ip;
            }
            else
            {
                _comssNegativeCache[domain] = DateTime.UtcNow.AddMinutes(10);
            }
        }


        // 4) Cloudflare
        {
            var (ip, _, _) = await ResolveViaDoh(domain, cloudflareDohUrl);
            if (ip != null)
            {
                _positiveCache[domain] = new(ip, DateTime.UtcNow.AddMinutes(5));
                return ip;
            }
        }

        // 5) Fallback 8.8.8.8
        var fallbackIp = ResolveViaUdp(domain, "8.8.8.8");
        if (fallbackIp != null)
            _positiveCache[domain] = new(fallbackIp, DateTime.UtcNow.AddMinutes(5));

        return fallbackIp;
    }
    private async Task<IPAddress?> ResolveViaDohWireFormat(string domain, string dohIp)
    {
        try
        {
            byte[] rawQuery = BuildDnsQuery(domain);

            string base64url = Convert.ToBase64String(rawQuery)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            string url = $"https://{dohIp}/dns-query?dns={base64url}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
            req.Headers.Add("Accept", "application/dns-message");

            var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                string err = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[DoH-WIRE] {url} => {resp.StatusCode} / {err}");
                return null;
            }

            byte[] responseBytes = await resp.Content.ReadAsByteArrayAsync();

            var response = DnsMessage.Parse(new ArraySegment<byte>(responseBytes));

            return response.AnswerRecords
                .OfType<ARecord>()
                .FirstOrDefault()
                ?.Address;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoH-WIRE] Ошибка: {ex.Message}");
            return null;
        }
    }

    private async Task<(IPAddress ip, bool isNxDomain, bool isServiceDown)> ResolveViaDoh(string domain, string template)
    {
        try
        {
            string url = string.Format(template, domain);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Accept", "application/dns-json");

            var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"DoH {url} => {resp.StatusCode}");
                return (null, false, true);
            }

            string json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Status", out var st) && st.GetInt32() != 0)
                return (null, true, false);

            if (doc.RootElement.TryGetProperty("Answer", out var answers) && answers.ValueKind == JsonValueKind.Array)
            {
                foreach (var rec in answers.EnumerateArray())
                {
                    if (rec.TryGetProperty("data", out var dataStr) &&
                        IPAddress.TryParse(dataStr.GetString(), out var ip))
                        return (ip, false, false);
                }
            }

            return (null, true, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DoH {template} => Ошибка: {ex.Message}");
            return (null, false, true);
        }
    }

    private static IPAddress? ResolveViaUdp(string domain, string dnsServer)
    {
        try
        {
            var client = new DnsClient(new[] { IPAddress.Parse(dnsServer) });

            if (!DomainName.TryParse(domain, out var dn))
                return null;

            var msg = client.Resolve(dn, RecordType.A);
            return msg?.AnswerRecords?.OfType<ARecord>().FirstOrDefault()?.Address;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP DNS {dnsServer} => {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Rule Checking

    private DnsDecision CheckRules(string clientIp, string domain)
    {
        MaybeReloadRules();

        foreach (var rule in _rulesCache)
        {
            if (!string.IsNullOrWhiteSpace(rule.SourceIp) && rule.SourceIp != clientIp)
                continue;

            if (rule.DomainPattern.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
            {
                string bare = rule.DomainPattern[2..]; // Убираем "*."

                bool isExact = domain.Equals(bare, StringComparison.OrdinalIgnoreCase);
                bool isSub = domain.EndsWith("." + bare, StringComparison.OrdinalIgnoreCase);

                if (!(isExact || isSub))
                    continue;
            }
            else if (!domain.Equals(rule.DomainPattern, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return rule.Action switch
            {
                "Block" => DnsDecision.Block,
                "Rewrite" when !string.IsNullOrWhiteSpace(rule.RewriteIp) =>
                    new DnsDecision { IsRewrite = true, RewriteIp = rule.RewriteIp },
                _ => DnsDecision.Allow
            };
        }

        return DnsDecision.Allow;
    }

    #endregion

    #region Cache & Rule Management

    private static void MaybeReloadRules()
    {
        if ((DateTime.UtcNow - _lastRulesLoaded).TotalSeconds > 60)
            ReloadRules();
    }

    private static void ReloadRules()
    {
        try
        {
            using var db = new DnsRulesContext();
            _rulesCache = db.DnsRules.ToList();
            _lastRulesLoaded = DateTime.UtcNow;
            Console.WriteLine($"[ReloadRules] Загружено правил: {_rulesCache.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ReloadRules] Ошибка: {ex.Message}");
        }
    }

    #endregion

    #region Timer Cleanup

    private void CleanupAndMaybeReloadRules()
    {
        try
        {
            foreach (var (key, value) in _positiveCache)
                if (value.Deadline < DateTime.UtcNow)
                    _positiveCache.TryRemove(key, out _);

            foreach (var (key, deadline) in _comssNegativeCache)
                if (deadline < DateTime.UtcNow)
                    _comssNegativeCache.TryRemove(key, out _);

            MaybeReloadRules();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CleanupTimer] Ошибка: {ex.Message}");
        }
    }

    #endregion
}
