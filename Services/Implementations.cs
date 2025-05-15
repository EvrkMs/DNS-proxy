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
    public async Task ClearStats()
    {
        var list = await db.Stats.ToListAsync();
        db.Stats.RemoveRange(list);
        await db.SaveChangesAsync();
    }
}