using System.Net;
using ARSoft.Tools.Net.Dns;

namespace DNS_proxy.Infrastructure;

public class DnsWireResolver : Core.Interfaces.IDnsResolver
{
    private readonly HttpClient _httpClient;

    public DnsWireResolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IPAddress?> ResolveAsync(string domain)
    {
        byte[] rawQuery = BuildDnsQuery(domain);
        string base64url = Convert.ToBase64String(rawQuery)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        string url = $"https://dns.comss.one/dns-query?dns={base64url}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
        req.Headers.Add("Accept", "application/dns-message");

        var resp = await _httpClient.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[DoH-WIRE] {url} => {resp.StatusCode} / {err}");
            return null;
        }

        var responseBytes = await resp.Content.ReadAsByteArrayAsync();
        var response = DnsMessage.Parse(new ArraySegment<byte>(responseBytes));
        return response.AnswerRecords.OfType<ARecord>().FirstOrDefault()?.Address;
    }

    private static byte[] BuildDnsQuery(string domain)
    {
        var rand = new Random();
        ushort transactionId = (ushort)rand.Next(ushort.MaxValue);
        List<byte> message = new();

        message.Add((byte)(transactionId >> 8));
        message.Add((byte)(transactionId & 0xFF));
        message.AddRange(new byte[] { 0x01, 0x00 }); // flags
        message.AddRange(new byte[] { 0x00, 0x01 }); // QDCOUNT
        message.AddRange(new byte[6]); // AN/NS/AR count

        foreach (var label in domain.Split('.'))
        {
            message.Add((byte)label.Length);
            message.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
        }
        message.Add(0x00);
        message.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01 }); // QTYPE=A, QCLASS=IN

        return message.ToArray();
    }
}
