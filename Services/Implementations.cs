using System.Net;
using DnsProxy.Data;
using DnsProxy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace DnsProxy.Services;

public class DnsConfigService(AppDbContext db) : IDnsConfigService
{
    public Task<List<DnsServerEntry>> GetAllAsync() => db.Servers.OrderBy(s => s.Priority).ToListAsync();
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

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    record Entry(IPAddress Ip, DateTime Exp);
    private readonly MemoryCache _inner = new(new MemoryCacheOptions());

    public bool TryGet(string key, out (IPAddress ip, int ttl) entry)
    {
        if (cache.TryGetValue<Entry>(key, out var e) && e.Exp > DateTime.UtcNow)
        {
            entry = (e.Ip, (int)(e.Exp - DateTime.UtcNow).TotalSeconds);
            return true;
        }
        entry = default;
        return false;
    }

    public void Set(string key, IPAddress ip, int ttl)
    {
        var e = new Entry(ip, DateTime.UtcNow.AddSeconds(ttl));
        cache.Set(key, e, TimeSpan.FromSeconds(ttl));
    }
    public void Clear()
    {
        // просто очищаем всё
        _inner.Compact(1.0);
    }
}