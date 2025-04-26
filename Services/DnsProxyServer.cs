using System.Net;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Models;
using DnsProxy.Services.Interfaces;
using DnsProxy.Utils;

namespace DnsProxy.Services;

public class DnsProxyServer : IDisposable
{
    private readonly DnsServer _server;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DnsProxyServer> _log;

    public DnsProxyServer(IServiceScopeFactory scopeFactory,
                          ILogger<DnsProxyServer> log)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        var bind = new IPEndPoint(IPAddress.Any, 53);
        _server = new DnsServer(bindEndPoint: bind,
                                udpListenerCount: 1,
                                tcpListenerCount: 0);

        _server.QueryReceived += OnQueryAsync;
    }

    public void Start() => _server.Start();
    public void Dispose() => _server.Stop();

    private async Task OnQueryAsync(object sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage req) return;

        var q = req.Questions.FirstOrDefault(x => x.RecordType == RecordType.A);
        if (q == null)
        {
            e.Response = req.CreateResponseInstance();
            return;
        }

        var domain = q.Name.ToString().TrimEnd('.');
        var clientIp = e.RemoteEndpoint?.Address.ToString() ?? "0.0.0.0";

        // здесь открываем scope и получаем все scoped-сервисы
        using var scope = _scopeFactory.CreateScope();
        var rules = scope.ServiceProvider.GetRequiredService<IRuleService>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var conf = scope.ServiceProvider.GetRequiredService<IDnsConfigService>();
        var stats = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
        var resolver = scope.ServiceProvider.GetRequiredService<IResolverService>();

        // собственно «конвейер»
        var (ip, ttl, upstream, rcode) =
            await ExecuteAsync(clientIp, domain, rules, cache, conf, stats, resolver);

        var resp = req.CreateResponseInstance();

        if (rcode == "BLOCK")
        {
            resp.ReturnCode = ReturnCode.Refused;
        }
        else if (ip == null)
        {
            resp.ReturnCode = ReturnCode.NxDomain;
        }
        else
        {
            resp.AnswerRecords.Add(new ARecord(q.Name, ttl, ip));
        }

        _log.LogInformation("DNS {domain} ← {client} => {ip} via {up} ({rc})",
                            domain, clientIp,
                            ip?.ToString() ?? "-", upstream, rcode);

        e.Response = resp;
    }

    private static async Task<(IPAddress? ip, int ttl, string upstream, string rcode)>
        ExecuteAsync(string clientIp,
                     string domain,
                     IRuleService rulesSvc,
                     ICacheService cache,
                     IDnsConfigService conf,
                     IStatisticsService stats,
                     IResolverService resolver)
    {
        // 1. правила
        var rules = await rulesSvc.GetAllAsync();
        var (act, rewrite, include, exclude) =
            RuleHelper.Apply(rules, clientIp, domain);

        if (act == RuleAction.Block)
            return (null, 0, "-", "BLOCK");

        if (act == RuleAction.Rewrite &&
            IPAddress.TryParse(rewrite, out var ipRw))
            return (ipRw, 60, "REWRITE", "NOERROR");

        // 2. кэш
        if (cache.TryGet(domain, out var c))
            return (c.ip, c.ttl, "CACHE", "NOERROR");

        // 3. upstream
        var pool = await RuleExtensions.FilterServers(conf, include, exclude);
        var (ip, ttl, up, diag) = await resolver.ResolveAsync(domain, pool);

        // 4. сохраняем в кэш, если есть ответ
        if (ip != null)
            cache.Set(domain, ip, ttl);

        // 5. статистика
        await stats.AddAsync(new VisitStatistic
        {
            Timestamp = DateTime.UtcNow,
            ClientIp = clientIp,
            Domain = domain,
            Upstream = up,
            Action = act
        });

        // rcode: либо текст из diag ("NOERROR","NXDOMAIN" или сообщение об ошибке)
        return (ip, ttl, up, ip == null && diag == "NOERROR" ? "NXDOMAIN" : diag);
    }
}
