using System.Collections.Concurrent;
using ARSoft.Tools.Net.Dns;

public interface ICacheService
{
    bool TryGet(string domain, RecordType type, out (DnsRecordBase[] records, int ttl) entry);
    void Set(string domain, RecordType type, DnsRecordBase[] records, int ttl);
    void Clear();
}
public class SimpleDnsCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, Entry> _cache = new();

    private class Entry
    {
        public DnsRecordBase[] Records { get; }
        public DateTime ExpireAt { get; }

        public Entry(DnsRecordBase[] records, int ttlSeconds)
        {
            Records = records;
            ExpireAt = DateTime.UtcNow.AddSeconds(ttlSeconds);
        }

        public int RemainingTtl => (int)(ExpireAt - DateTime.UtcNow).TotalSeconds;
        public bool IsExpired => DateTime.UtcNow >= ExpireAt;
    }

    private static string GetKey(string domain, RecordType type)
        => $"{domain}#{type}";

    public bool TryGet(string domain, RecordType type, out (DnsRecordBase[] records, int ttl) entry)
    {
        var key = GetKey(domain, type);
        if (_cache.TryGetValue(key, out var val))
        {
            if (val.IsExpired)
            {
                _cache.TryRemove(key, out _);
                entry = default;
                return false;
            }

            entry = (val.Records, val.RemainingTtl);
            return true;
        }

        entry = default;
        return false;
    }

    public void Set(string domain, RecordType type, DnsRecordBase[] records, int ttl)
    {
        var key = GetKey(domain, type);
        _cache[key] = new Entry(records, ttl);
    }

    public void Clear() => _cache.Clear();
}
