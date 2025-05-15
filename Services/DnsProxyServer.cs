// -----------------------------------------------------------------------------
//  DnsProxyServer.cs      DNS-проксирующий сервер c per-domain circuit-breaker
// -----------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Threading;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Models;
using DnsProxy.Utils;

namespace DnsProxy.Services;

public sealed class DnsProxyServer : IDisposable
{
    /*───────────────────────────────────────────────────────────────────────────*/
    #region circuit-breaker storage
    /*  Ключ  = (domain, upstream)
        Val   = информация о неудачах / бане                                  */
    private record FailInfo(int Count, DateTime? BannedUntil);
    private const int FORCE_RETRY_MAX = 10;
    #endregion
    /*───────────────────────────────────────────────────────────────────────────*/

    private readonly DnsServer _server;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DnsProxyServer> _log;

    /*-------------------------------------------------------------------------*/
    public DnsProxyServer(IServiceScopeFactory scopeFactory,
                          ILogger<DnsProxyServer> log)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        var bind = new IPEndPoint(IPAddress.Any, 53);
        _server = new DnsServer(bindEndPoint: bind, udpListenerCount: 1, tcpListenerCount: 0);

        _server.QueryReceived += OnQueryAsync;
    }

    public void Start() { _server.Start(); StartForceCacheUpdater(); }
    public void Dispose() => _server.Stop();

    /*-------------------------------------------------------------------------*/
    private async Task OnQueryAsync(object? sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage req) return;

        string clientIp = e.RemoteEndpoint?.Address.ToString() ?? "0.0.0.0";
        var resp = req.CreateResponseInstance();

        foreach (var q in req.Questions)
        {
            string domain = q.Name.ToString().TrimEnd('.');
            var result = await ExecuteAsync(clientIp, domain, q.RecordType);

            if (result.RCode == "BLOCK")
            {
                resp.AnswerRecords.Add(new ARecord(q.Name, 60, IPAddress.Any));
            }
            else if (result.Records.Length == 0)
            {
                resp.ReturnCode = ReturnCode.NxDomain;
            }
            else
            {
                foreach (var rec in result.Records)
                    resp.AnswerRecords.Add(rec);
            }

            _log.LogInformation("DNS {dom} [{type}] ← {cli} ⇒ {ans} via {up} ({rc})",
                domain, q.RecordType, clientIp,
                result.Records.Length > 0 ? result.Records[0].ToString() : "-",
                result.Upstream, result.RCode);
        }

        e.Response = resp;
    }

    /*-------------------------------------------------------------------------*/
    private async Task<DnsResolveResult> ExecuteAsync(string clientIp, string domain, RecordType type)
    {
        using var scope = _scopeFactory.CreateScope();
        var rulesSvc = scope.ServiceProvider.GetRequiredService<IRuleService>();
        var resolver = scope.ServiceProvider.GetRequiredService<IResolverService>();
        var cfg = scope.ServiceProvider.GetRequiredService<IDnsConfigService>();
        var stats = scope.ServiceProvider.GetRequiredService<IStatisticsService>();

        var rules = await rulesSvc.GetAllAsync();
        var (act, rewrite, includeCsv, excludeCsv, forceId) =
            RuleHelper.Apply(rules, clientIp, domain);

        if (act == RuleAction.Block)
            return DnsResolveResult.Empty("-", "BLOCK");

        if (act == RuleAction.Rewrite && IPAddress.TryParse(rewrite, out var ipRw))
        {
            var rec = new ARecord(DomainName.Parse(domain), 120, ipRw);
            return DnsResolveResult.Success([rec], 120, "REWRITE", type);
        }

        var pool = await cfg.FilterServers(includeCsv, excludeCsv, forceId);

        DnsResolveResult result;

        if (forceId is not null && pool.Count == 1)
        {
            result = DnsResolveResult.Empty(pool[0].Address!, "NXDOMAIN");

            for (int i = 0; i < FORCE_RETRY_MAX; i++)
            {
                result = await resolver.ResolveAsync(domain, type, pool);
                if (result.Records.Length > 0)
                    break;
            }
        }
        else
        {
            result = await resolver.ResolveAsync(domain, type, pool);
        }

        await stats.AddAsync(new VisitStatistic
        {
            Timestamp = DateTime.UtcNow,
            ClientIp = clientIp,
            Domain = domain,
            Upstream = result.Upstream,
            Rcode = result.RCode,
            Action = act,
            Type = result.Type
        });

        return result;
    }


    public void StartForceCacheUpdater()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                using var scope = _scopeFactory.CreateScope();
                var rulesSvc = scope.ServiceProvider.GetRequiredService<IRuleService>();
                var resolver = scope.ServiceProvider.GetRequiredService<IResolverService>();
                var conf = scope.ServiceProvider.GetRequiredService<IDnsConfigService>();

                var rules = await rulesSvc.GetAllAsync();
                var forceRules = rules.Where(r => r.ForceServerId is not null).ToList();

                var now = DateTime.UtcNow;
                int? minTtl = null;

                foreach (var rule in forceRules)
                {
                    var allDomains = rule.DomainPattern
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(d => d.TrimStart('*').Trim('.'))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    var pool = await conf.FilterServers(rule.IncludeServers, rule.ExcludeServers, rule.ForceServerId);
                    if (pool.Count == 0)
                        continue;

                    foreach (var dom in allDomains)
                    {
                        // ⛳ по умолчанию кэшируем только A-записи
                        var type = RecordType.A;

                        var result = await resolver.ResolveAsync(dom, type, pool);
                        if (result.Records.Length > 0 && result.RCode == "NOERROR")
                        {
                            _log.LogInformation("Force cache updated: {domain} → {ip} (TTL: {ttl})",
                                dom, string.Join(", ", result.Records.Select(r => r.ToString())), result.Ttl);

                            if (minTtl is null || result.Ttl < minTtl)
                                minTtl = result.Ttl;
                        }
                        else
                        {
                            _log.LogWarning("Failed to update force cache for domain: {domain}", dom);
                        }
                    }
                }

                var delay = TimeSpan.FromSeconds(minTtl ?? 10);
                await Task.Delay(delay);
            }
        });
    }
}
