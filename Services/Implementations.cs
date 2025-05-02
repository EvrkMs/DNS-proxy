using System.Collections.Concurrent;
using System.Net;
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
    private readonly MemoryCache Cache = new(new MemoryCacheOptions
    {
        SizeLimit = 4048 // Устанавливаем лимит размера кэша
    });
    private readonly ConcurrentDictionary<string, bool> Keys = new();

    private record Entry(IPAddress Ip, DateTime Exp);

    public bool TryGet(string key, out (IPAddress ip, int ttl) entry)
    {
        if (Cache.TryGetValue<Entry>(key, out var e) && e.Exp > DateTime.UtcNow)
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
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl),
            Size = 1 // Размер записи
        };

        // Регистрируем обратный вызов при удалении элемента из кэша
        options.RegisterPostEvictionCallback((evictedKey, value, reason, state) =>
        {
            Keys.TryRemove(evictedKey.ToString(), out _);
        });

        Cache.Set(key, e, options);
        Keys[key] = true;
    }

    public void Clear()
    {
        Cache.Compact(1.0); // Удаляем все записи из кэша
        Keys.Clear(); // Очищаем список ключей
    }

    public IEnumerable<string> GetAllKeys()
    {
        return Keys.Keys;
    }
    public IEnumerable<(string Key, IPAddress Ip, int Ttl)> GetAllEntries()
    {
        foreach (var key in Keys.Keys)
        {
            if (Cache.TryGetValue<Entry>(key, out var e))
            {
                var ttl = (int)(e.Exp - DateTime.UtcNow).TotalSeconds;
                if (ttl > 0)
                    yield return (key, e.Ip, ttl);
            }
        }
    }
}