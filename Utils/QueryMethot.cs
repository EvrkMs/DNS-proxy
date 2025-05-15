using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Models;
using DnsProxy.Services;

namespace DnsProxy.Utils;

public class QueryMethot(
    ILogger<QueryMethot> _log,
    IHttpClientPerServerService httpPerServer,
    ICacheService cache)
{
    private static readonly Random _rnd = new();
    private static DnsRecordBase? TryParseJsonRecord(RecordType rt, string data, DomainName name, int ttl)
    {
        return rt switch
        {
            RecordType.A when IPAddress.TryParse(data, out var ip4) => new ARecord(name, ttl, ip4),
            RecordType.Aaaa when IPAddress.TryParse(data, out var ip6) => new AaaaRecord(name, ttl, ip6),
            RecordType.Txt => new TxtRecord(name, ttl, data),
            RecordType.Mx => TryParseMx(data, name, ttl),
            RecordType.Ptr => new PtrRecord(name, ttl, DomainName.Parse(data)),
            RecordType.CName => new CNameRecord(name, ttl, DomainName.Parse(data)),
            RecordType.Ns => new NsRecord(name, ttl, DomainName.Parse(data)),
            _ => null
        };
    }
    
    private static DnsRecordBase? TryParseMx(string data, DomainName name, int ttl)
    {
        var parts = data.Split(' ', 2);
        if (parts.Length != 2 || !ushort.TryParse(parts[0], out var pref))
            return null;
        return new MxRecord(name, ttl, pref, DomainName.Parse(parts[1]));
    }
    public async Task<(DnsRecordBase[] records, int ttl)> QueryUdpAsync(string domain, RecordType type, DnsServerEntry s)
    {
        if (cache.TryGet(domain, type, out var cached))
            return cached;

        var resolver = new DnsStubResolver([IPAddress.Parse(s.Address!)], 3000);
        var records = await resolver.ResolveAsync<DnsRecordBase>(
            DomainName.Parse(domain), type, RecordClass.INet
        );

        if (records == null || records.Count == 0)
            return (Array.Empty<DnsRecordBase>(), 0);

        var ttl = records.Min(r => r.TimeToLive);

        cache.Set(domain, type, [.. records], ttl);
        return (records.ToArray(), ttl);
    }

    public async Task<(DnsRecordBase[] records, int ttl)> QueryDoHWireManualAsync(string domain, RecordType type, DnsServerEntry s)
    {
        if (cache.TryGet(domain, type, out var cached))
            return cached;

        var baseAddr = s.Address.TrimEnd('/');
        if (!baseAddr.Contains('/'))
            baseAddr += "/dns-query";

        byte[] wire = BuildWireQuery(domain, type);
        string b64 = Convert.ToBase64String(wire).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var url = $"{baseAddr}?dns={b64}";

        var http = httpPerServer.GetOrCreate(s);
        var req = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-message"));

        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            _log.LogWarning("[DoH-Wire] {url} => {code} / {body}", url, resp.StatusCode, body);
            return ([], 0);
        }

        var data = await resp.Content.ReadAsByteArrayAsync();
        var msg = DnsMessage.Parse(data);
        var relevant = msg.AnswerRecords.Where(r => r.RecordType == type).ToArray();
        var ttl = relevant.Length > 0 ? relevant.Min(r => (int)r.TimeToLive) : 0;

        if (ttl > 0 && relevant.Length > 0)
            cache.Set(domain, type, relevant, ttl);

        return (relevant, ttl);
    }

    public async Task<(DnsRecordBase[] records, int ttl)> QueryDoHJsonAsync(string domain, RecordType type, DnsServerEntry s)
    {
        if (cache.TryGet(domain, type, out var cached))
            return cached;

        var baseAddr = s.Address.TrimEnd('/');
        if (!baseAddr.Contains('/'))
            baseAddr += "/resolve";

        var ub = new UriBuilder(baseAddr);
        var qs = System.Web.HttpUtility.ParseQueryString(ub.Query);
        qs["name"] = domain;
        qs["type"] = ((ushort)type).ToString();
        ub.Query = qs.ToString();

        var http = httpPerServer.GetOrCreate(s);
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

        var resp = await http.GetAsync(ub.Uri);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            _log.LogWarning("[DoH-Json] {url} => {code} / {body}", ub.Uri, resp.StatusCode, body);
            return ([], 0);
        }

        using var doc = await JsonSerializer.DeserializeAsync<JsonDocument>(
            await resp.Content.ReadAsStreamAsync());
        if (doc == null || !doc.RootElement.TryGetProperty("Answer", out var answers))
            return ([], 0);

        var records = new List<DnsRecordBase>();
        int ttl = int.MaxValue;

        foreach (var el in answers.EnumerateArray())
        {
            if (!Enum.TryParse<RecordType>(el.GetProperty("type").GetInt32().ToString(), out var rt))
                continue;

            var name = DomainName.Parse(domain);
            var ttlVal = el.GetProperty("TTL").GetInt32();
            ttl = Math.Min(ttl, ttlVal);

            var data = el.GetProperty("data").GetString();
            var parsed = TryParseJsonRecord(rt, data!, name, ttlVal);
            if (parsed != null)
                records.Add(parsed);
        }

        if (records.Count > 0 && ttl != int.MaxValue)
            cache.Set(domain, type, records.ToArray(), ttl);

        return (records.ToArray(), ttl == int.MaxValue ? 0 : ttl);
    }

    private static byte[] BuildWireQuery(string domain, RecordType type)
    {
        ushort id = (ushort)_rnd.Next(0, 0x10000);
        var header = new byte[]
        {
            (byte)(id >> 8), (byte)id,
            0x01, 0x00, 0x00, 0x01,  // QDCOUNT=1
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        var q = new List<byte>();
        foreach (var lbl in domain.Split('.'))
        {
            var bs = Encoding.ASCII.GetBytes(lbl);
            q.Add((byte)bs.Length);
            q.AddRange(bs);
        }
        q.Add(0x00); // конец имени
        q.AddRange(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)type))); // QTYPE
        q.Add(0x00); q.Add(0x01); // QCLASS = IN

        return header.Concat(q).ToArray();
    }
}
