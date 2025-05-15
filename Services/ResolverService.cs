// File: Services/Interfaces/IResolverService.cs
using ARSoft.Tools.Net.Dns;
using DnsProxy.Models;
using DnsProxy.Utils;

namespace DnsProxy.Services;

public interface IResolverService
{
    Task<DnsResolveResult> ResolveAsync(
        string domain,
        RecordType type,
        List<DnsServerEntry> pool,
        CancellationToken cancellationToken = default
    );
}

public class ResolverService(
    ILogger<ResolverService> log,
    IConfigService config,
    QueryMethot queryMethot
) : IResolverService
{
    private readonly ILogger<ResolverService> _log = log ?? throw new ArgumentNullException(nameof(log));

    public async Task<DnsResolveResult> ResolveAsync(
        string domain,
        RecordType type,
        List<DnsServerEntry> pool,
        CancellationToken cancellationToken = default)
    {
        if (pool.Count == 0)
            return DnsResolveResult.Empty("-", "Empty pool");

        var conf = await config.GetConfigAsync(cancellationToken);
        var strategy = conf?.Strategy ?? ResolveStrategy.FirstSuccess;

        return strategy switch
        {
            ResolveStrategy.FirstSuccess => await ResolveSequentially(domain, type, pool),
            ResolveStrategy.ParallelAll => await ResolveInParallel(domain, type, pool),
            _ => throw new NotSupportedException($"Strategy {strategy} not supported"),
        };
    }

    private async Task<DnsResolveResult> ResolveSequentially(string domain, RecordType type, List<DnsServerEntry> pool)
    {
        Exception? lastEx = null;

        foreach (var server in pool.OrderBy(p => p.Priority))
        {
            try
            {
                var (records, ttl) = await ResolveOne(domain, type, server);

                if (records.Length > 0)
                    return DnsResolveResult.Success(records, ttl, server.Address!, type);

                _log.LogWarning("[{addr}] returned NXDOMAIN for {domain}", server.Address, domain);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                _log.LogWarning(ex, "Upstream {addr} failed", server.Address);
            }
        }

        string upstream = pool.Last().Address!;
        string errorMsg = lastEx?.GetBaseException().Message ?? "NXDOMAIN";

        return DnsResolveResult.Empty(upstream, errorMsg);
    }

    private async Task<DnsResolveResult> ResolveInParallel(string domain, RecordType type, List<DnsServerEntry> pool)
    {
        var tasks = pool.Select(server => Task.Run(async () =>
        {
            try
            {
                var (records, ttl) = await ResolveOne(domain, type, server);
                return records.Length > 0
                    ? DnsResolveResult.Success(records, ttl, server.Address!, type)
                    : DnsResolveResult.Empty(server.Address!, "NXDOMAIN");
            }
            catch (Exception ex)
            {
                return DnsResolveResult.Empty(server.Address!, ex.GetBaseException().Message);
            }
        })).ToList();

        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            tasks.Remove(finished);

            var result = await finished;
            if (result.Records.Length > 0)
                return result;
        }

        return DnsResolveResult.Empty(pool.Last().Address!, "NXDOMAIN");
    }

    private Task<(DnsRecordBase[] records, int ttl)> ResolveOne(string domain, RecordType type, DnsServerEntry server)
    {
        return server.Protocol switch
        {
            DnsProtocol.Udp => queryMethot.QueryUdpAsync(domain, type, server),
            DnsProtocol.DoH_Wire => queryMethot.QueryDoHWireManualAsync(domain, type, server),
            DnsProtocol.DoH_Json => queryMethot.QueryDoHJsonAsync(domain, type, server),
            _ => throw new NotSupportedException($"Protocol {server.Protocol} not supported")
        };
    }
}

public class DnsResolveResult
{
    public DnsRecordBase[] Records { get; set; } = [];
    public int Ttl { get; set; }
    public string Upstream { get; set; } = "-";
    public string RCode { get; set; } = "NXDOMAIN";
    public RecordType Type { get; set; } = RecordType.Invalid;
    public static DnsResolveResult Empty(string upstream = "-", string rcode = "NXDOMAIN", RecordType Type = RecordType.Invalid)
    => new () { Upstream = upstream, RCode = rcode };

    public static DnsResolveResult Success(DnsRecordBase[] records, int ttl, string upstream, RecordType Type)
        => new() { Records = records, Ttl = ttl, Upstream = upstream, RCode = "NOERROR", Type = Type };
}
