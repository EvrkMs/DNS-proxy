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
    Task ClearStats();
    Task AddAsync(VisitStatistic stat);
}