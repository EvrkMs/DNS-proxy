// -----------------------------------------------------------------------------
//  DnsProxyServer.cs      DNS-проксирующий сервер c per-domain circuit-breaker
// -----------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Net;
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

    private static readonly ConcurrentDictionary<(string domain, string up), FailInfo>
        _fails = new();

    private const int failThreshold = 5;                 // после 5 ошибок подряд
    private static readonly TimeSpan banDuration = TimeSpan.FromMinutes(10);
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

    public void Start() => _server.Start();
    public void Dispose() => _server.Stop();

    /*-------------------------------------------------------------------------*/
    private async Task OnQueryAsync(object? sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage req) return;

        var q = req.Questions.FirstOrDefault(z => z.RecordType == RecordType.A);
        if (q is null) { e.Response = req.CreateResponseInstance(); return; }

        string domain = q.Name.ToString().TrimEnd('.');
        string clientIp = e.RemoteEndpoint?.Address.ToString() ?? "0.0.0.0";

        // ── достаём scoped-сервисы
        using var scope = _scopeFactory.CreateScope();
        var rules = scope.ServiceProvider.GetRequiredService<IRuleService>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var conf = scope.ServiceProvider.GetRequiredService<IDnsConfigService>();
        var stats = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
        var resolver = scope.ServiceProvider.GetRequiredService<IResolverService>();

        // ── конвейер
        var (ip, ttl, upstream, rcode) =
            await ExecuteAsync(clientIp, domain, rules, cache, conf, stats, resolver);

        // ── формируем ответ
        var resp = req.CreateResponseInstance();

        if (rcode == "BLOCK")
        {
            // отправляем «чёрную дыру» (0.0.0.0)
            resp.AnswerRecords.Add(new ARecord(q.Name, 60, IPAddress.Any));
        }
        else if (ip is null)
        {
            resp.ReturnCode = ReturnCode.NxDomain;
        }
        else
        {
            resp.AnswerRecords.Add(new ARecord(q.Name, ttl, ip));
        }

        _log.LogInformation("DNS {dom} ← {cli} ⇒ {ans} via {up} ({rc})",
                            domain, clientIp, ip?.ToString() ?? "-", upstream, rcode);

        e.Response = resp;
    }

    /*-------------------------------------------------------------------------*/
    private static async Task<(IPAddress? ip, int ttl, string upstream, string rcode)>
        ExecuteAsync(string clientIp,
                     string domain,
                     IRuleService rulesSvc,
                     ICacheService cache,
                     IDnsConfigService cfg,
                     IStatisticsService stats,
                     IResolverService resolver)
    {
        /* ① ─ правила */
        var rules = await rulesSvc.GetAllAsync();
        var (act, rewrite, includeCsv, excludeCsv, forceId) =
            RuleHelper.Apply(rules, clientIp, domain);

        if (act == RuleAction.Block)
            return (null, 0, "-", "BLOCK");

        if (act == RuleAction.Rewrite &&
            IPAddress.TryParse(rewrite, out var ipRw))
            return (ipRw, 60, "REWRITE", "NOERROR");

        /* ② ─ кэш */
        if (cache.TryGet(domain, out var c))
            return (c.ip, c.ttl, "CACHE", "NOERROR");

        /* ③ ─ апстрим-пул (include / exclude / force) */
        var pool = await cfg.FilterServers(includeCsv, excludeCsv, forceId);

        /* ④ ─ резолв (+ до 10 ретраев для Force-сервера) */
        IPAddress? ip = null;
        int ttl = 0;
        string up = "-";
        string diag = "NXDOMAIN";

        if (forceId is not null && pool.Count == 1)
        {
            for (int i = 0; i < FORCE_RETRY_MAX; i++)
            {
                (ip, ttl, up, diag) = await resolver.ResolveAsync(domain, pool);
                if (ip is not null) break;               // успех
            }
            if (ip is null) diag = "NXDOMAIN";
        }
        else
        {
            (ip, ttl, up, diag) = await resolver.ResolveAsync(domain, pool);
        }

        /* ⑤ ─ кэширование */
        if (ip is not null) cache.Set(domain, ip, ttl);

        /* ⑥ ─ статистика */
        await stats.AddAsync(new VisitStatistic
        {
            Timestamp = DateTime.UtcNow,
            ClientIp = clientIp,
            Domain = domain,
            Upstream = up,
            Rcode = diag,
            Action = act
        });

        return (ip, ttl, up,
                ip is null && diag == "NOERROR" ? "NXDOMAIN" : diag);
    }
}
