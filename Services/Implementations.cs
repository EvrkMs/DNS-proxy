using System.Collections.Concurrent;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Data;
using DnsProxy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace DnsProxy.Services;

public class DnsConfigService(AppDbContext db) : IDnsConfigService
{
    public async Task<List<DnsServerEntry>> GetAllAsync() => await db.Servers.OrderBy(s => s.Priority).ToListAsync();
}

public class RuleService(AppDbContext db) : IRuleService
{
    public async Task<List<DnsRule>> GetAllAsync(bool includeForce = false)
    {
        IQueryable<DnsRule> q = db.Rules;
        if (includeForce) q = q.Include(r => r.ForceServer);
        return await q.ToListAsync();
    }
}

public class StatisticsService(AppDbContext db) : IStatisticsService
{
    public async Task AddAsync(VisitStatistic s)
    {
        db.Stats.Add(s);
        await db.SaveChangesAsync();
    }
}

public class MemoryCacheService : ICacheService
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 4048
    });

    private readonly ConcurrentDictionary<string, bool> _keys = new();

    private record Entry(DnsRecordBase[] Records, DateTime Exp);

    private static string GetFullKey(string domain, RecordType type) => $"{domain}#{type}";

    public bool TryGet(string domain, RecordType type, out (DnsRecordBase[] records, int ttl) entry)
    {
        var key = GetFullKey(domain, type);
        if (_cache.TryGetValue<Entry>(key, out var e) && e.Exp > DateTime.UtcNow)
        {
            var ttl = (int)(e.Exp - DateTime.UtcNow).TotalSeconds;
            entry = (e.Records, ttl);
            return true;
        }
        entry = default;
        return false;
    }

    public void Set(string domain, RecordType type, DnsRecordBase[] records, int ttl)
    {
        var key = GetFullKey(domain, type);
        var e = new Entry(records, DateTime.UtcNow.AddSeconds(ttl));
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl),
            Size = 1
        };

        options.RegisterPostEvictionCallback((evictedKey, value, reason, state) =>
        {
            _keys.TryRemove(evictedKey.ToString(), out _);
        });

        _cache.Set(key, e, options);
        _keys[key] = true;
    }

    public void Clear()
    {
        _cache.Compact(1.0);
        _keys.Clear();
    }

    public IEnumerable<string> GetAllKeys()
    {
        return _keys.Keys;
    }

    public IEnumerable<(string Key, DnsRecordBase[] Records, int Ttl)> GetAllEntries()
    {
        foreach (var key in _keys.Keys)
        {
            if (_cache.TryGetValue<Entry>(key, out var e))
            {
                var ttl = (int)(e.Exp - DateTime.UtcNow).TotalSeconds;
                if (ttl > 0)
                    yield return (key, e.Records, ttl);
            }
        }
    }
}
