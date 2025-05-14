using ARSoft.Tools.Net.Dns;
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
    bool TryGet(string domain, RecordType type, out (DnsRecordBase[] records, int ttl) entry);
    void Set(string domain, RecordType type, DnsRecordBase[] records, int ttl);
    void Clear();
    IEnumerable<string> GetAllKeys();
    IEnumerable<(string Key, DnsRecordBase[] Records, int Ttl)> GetAllEntries();
}
