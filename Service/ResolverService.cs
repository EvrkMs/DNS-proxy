using System.Net;
using System.Text.Json;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using DNS_proxy.Core.Interfaces;
using DNS_proxy.Core.Models;
using DNS_proxy.Data;
using DNS_proxy.Utils;

namespace DNS_proxy.Service;

public class ResolverService : IResolverService
{
    private readonly HttpClient _httpClient = new();
    private List<DnsServerEntry> _servers;

    private readonly Dictionary<string, Dictionary<string, CacheEntry>> _serverCache = [];
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public ResolverService()
    {
        using var db = new DnsRulesContext();
        _servers = db.DnsServers.OrderBy(s => s.Priority).ToList();

        foreach (var s in _servers)
            _serverCache[s.Address] = [];
    }
    public void RestoreDnsServer()
    {
        using var db = new DnsRulesContext();
        var list = db.DnsServers.OrderBy(s => s.Priority).ToList();
        if (list.Count > 0)
        {
            _servers.Clear();
            _servers = list;
            foreach (var s in _servers)
                _serverCache[s.Address] = [];
        }
    }

    public async Task<IPAddress?> ResolveAsync(string domain)
    {
        foreach (var server in _servers)
        {
            if (_serverCache.TryGetValue(server.Address, out var cache) &&
                cache.TryGetValue(domain, out var entry) &&
                entry.Deadline > DateTime.UtcNow)
            {
                Logger.Log($"[Cache] {domain} => {entry.IP} (from {server.Address})");
                return entry.IP;
            }

            try
            {
                IPAddress? resolved = server.IsDoh switch
                {
                    true when server.UseWireFormat => await ResolveViaDohWireFormat(domain, server.Address),
                    true => await ResolveViaDohJson(domain, server.Address),
                    false => ResolveViaUdp(domain, new[] { server.Address })
                };

                if (resolved != null)
                {
                    Logger.Log($"[Resolve] {domain} => {resolved} via {server.Address}");
                    _serverCache[server.Address][domain] = new CacheEntry(resolved, DateTime.UtcNow + _cacheDuration);
                    return resolved;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[Resolver] Ошибка на {server.Address}: {ex.Message}");
            }
        }

        return null;
    }

    private static IPAddress? ResolveViaUdp(string domain, IEnumerable<string> dnsServers)
    {
        foreach (var server in dnsServers)
        {
            try
            {
                var ip = IPAddress.Parse(server);
                var client = new DnsClient([ip]);

                if (!DomainName.TryParse(domain, out var dn))
                    continue;

                var msg = client.Resolve(dn, RecordType.A);
                var result = msg?.AnswerRecords?.OfType<ARecord>().FirstOrDefault()?.Address;
                if (result != null)
                    return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UDP] {server} => {ex.Message}");
            }
        }

        return null;
    }
    private async Task<IPAddress?> ResolveViaDohWireFormat(string domain, string baseAddress)
    {
        try
        {
            if (!baseAddress.Contains("/"))
                baseAddress += "/dns-query"; // ← добавляем путь, если его нет

            byte[] rawQuery = BuildDnsQuery(domain);
            string base64url = Convert.ToBase64String(rawQuery)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            string url = $"{(baseAddress.EndsWith("/") ? baseAddress.TrimEnd('/') : baseAddress)}?dns={base64url}";

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
                Logger.Log($"[DoH-WIRE] {url} => {resp.StatusCode} / {err}");
                return null;
            }

            byte[] responseBytes = await resp.Content.ReadAsByteArrayAsync();
            var response = DnsMessage.Parse(new ArraySegment<byte>(responseBytes));
            return response.AnswerRecords.OfType<ARecord>().FirstOrDefault()?.Address;
        }
        catch (Exception ex)
        {
            Logger.Log($"[DoH-WIRE] Ошибка: {ex.Message}");
            return null;
        }
    }

    private async Task<IPAddress?> ResolveViaDohJson(string domain, string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{url}?name={domain}&type=A");
        req.Headers.Add("Accept", "application/dns-json");

        var resp = await _httpClient.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;

        string json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("Answer", out var answers))
        {
            foreach (var rec in answers.EnumerateArray())
            {
                if (rec.TryGetProperty("data", out var dataStr) &&
                    IPAddress.TryParse(dataStr.GetString(), out var ip))
                    return ip;
            }
        }

        return null;
    }
    private static byte[] BuildDnsQuery(string domain)
    {
        var rand = new Random();
        ushort transactionId = (ushort)rand.Next(ushort.MaxValue);
        List<byte> message =
        [
            (byte)(transactionId >> 8),
        (byte)(transactionId & 0xFF),
        0x01, 0x00, // flags
        0x00, 0x01, // QDCOUNT
        0x00, 0x00, // ANCOUNT
        0x00, 0x00, // NSCOUNT
        0x00, 0x00  // ARCOUNT
        ];

        foreach (var label in domain.Split('.'))
        {
            message.Add((byte)label.Length);
            message.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
        }

        message.Add(0x00);         // end of QNAME
        message.Add(0x00); message.Add(0x01); // QTYPE A
        message.Add(0x00); message.Add(0x01); // QCLASS IN
        return [.. message];
    }
}
