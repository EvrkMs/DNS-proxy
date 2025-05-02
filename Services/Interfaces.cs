using System.Net;
using DnsProxy.Models;

namespace DnsProxy.Services;

public interface IDnsConfigService
{
    Task<List<DnsServerEntry>> GetAllAsync();
}
public interface IRuleService
{
    Task<List<DnsRule>> GetAllAsync(bool includeForce = false);
}
public interface IStatisticsService
{
    Task AddAsync(VisitStatistic stat);
}
public interface ICacheService
{
    bool TryGet(string key, out (System.Net.IPAddress ip, int ttl) entry);
    void Set(string key, System.Net.IPAddress ip, int ttl);
    void Clear();
    IEnumerable<string> GetAllKeys();
    IEnumerable<(string Key, IPAddress Ip, int Ttl)> GetAllEntries();
}
