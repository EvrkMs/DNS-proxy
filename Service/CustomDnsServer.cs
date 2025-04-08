using System.Net;
using ARSoft.Tools.Net.Dns;
using DNS_proxy.Core.Interfaces;
using DNS_proxy.Core.Models;
using DNS_proxy.Utils;
using Timer = System.Timers.Timer;

namespace DNS_proxy.Service;

public class CustomDnsServer : IDisposable
{
    private readonly IDnsConfigService _configService;
    private readonly IRuleService _ruleService;
    private readonly IResolverService _resolverService;

    private DnsServer? _dnsServer;
    private readonly Timer _reloadTimer;
    private List<DnsRule> _rules = [];

    public event Action<string>? OnLog;

    public CustomDnsServer(IDnsConfigService _configService, IRuleService _ruleService, IResolverService _resolverService)
    {
        this._configService = _configService;
        this._ruleService = _ruleService;
        this._resolverService = _resolverService;

        Logger.OnLog = Log;
        _rules = _ruleService.GetAllRules();

        _reloadTimer = new Timer(60_000);
        _reloadTimer.Elapsed += (_, _) => ReloadRules();
        _reloadTimer.Start();
    }

    public void Start()
    {
        _dnsServer = new DnsServer(53);
        _dnsServer.QueryReceived += OnQueryReceived;
        _dnsServer.Start();
        Log("DNS-сервер запущен на порту 53.");
    }

    public void Stop()
    {
        _dnsServer?.Stop();
        Log("DNS-сервер остановлен.");
    }

    public void Dispose()
    {
        Stop();
        _reloadTimer.Stop();
        _reloadTimer.Dispose();
    }

    private void Log(string msg)
    {
        OnLog?.Invoke($"[{DateTime.Now:T}] {msg}");
    }

    private void ReloadRules()
    {
        try
        {
            _rules = _ruleService.GetAllRules();
            Logger.Log($"[ReloadRules] Загружено правил: {_rules.Count}");
        }
        catch (Exception ex)
        {
            Logger.Log($"[ReloadRules] Ошибка: {ex.Message}");
        }
    }
    public void RestoreDnsServer()
    {
        try
        {
            _resolverService.RestoreDnsServer();
        }
        catch (Exception ex)
        {
            Logger.Log("Ошибка обновления списка серверов");
        }
    }

    private async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage request)
            return;

        var clientIp = e.RemoteEndpoint?.Address?.MapToIPv4().ToString() ?? "0.0.0.0";
        DnsMessage response = request.CreateResponseInstance();

        foreach (var question in request.Questions.Where(q => q.RecordType == RecordType.A))
        {
            string domain = question.Name.ToString().TrimEnd('.');
            Logger.Log($"DNS-запрос от {clientIp} => {domain}");

            var decision = ApplyRules(clientIp, domain);

            if (decision.IsBlocked)
            {
                Logger.Log("   -> BLOCK (NXDOMAIN)");
                response.ReturnCode = ReturnCode.NxDomain;
                continue;
            }

            if (decision.IsRewrite && IPAddress.TryParse(decision.RewriteIp, out var rewriteIp))
            {
                Logger.Log($"   -> REWRITE => {rewriteIp}");
                response.AnswerRecords.Add(new ARecord(question.Name, 60, rewriteIp));
                continue;
            }

            var resolvedIp = await _resolverService.ResolveAsync(domain);
            if (resolvedIp != null)
            {
                Logger.Log($"   -> REAL => {resolvedIp}");
                response.AnswerRecords.Add(new ARecord(question.Name, 60, resolvedIp));
            }
            else
            {
                Logger.Log("   -> NXDOMAIN (не найден)");
                response.ReturnCode = ReturnCode.NxDomain;
            }
        }

        e.Response = response;
    }

    private DnsDecision ApplyRules(string clientIp, string domain)
    {
        foreach (var rule in _rules)
        {
            if (!string.IsNullOrWhiteSpace(rule.SourceIp) &&
                !Utils.IpMatchHelper.IsIpMatch(clientIp, rule.SourceIp))
                continue;

            if (rule.DomainPattern.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
            {
                string bare = rule.DomainPattern[2..];
                if (!domain.EndsWith("." + bare, StringComparison.OrdinalIgnoreCase) &&
                    !domain.Equals(bare, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else if (!domain.Equals(rule.DomainPattern, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return rule.Action switch
            {
                "Block" => DnsDecision.Block,
                "Rewrite" when !string.IsNullOrWhiteSpace(rule.RewriteIp) => new DnsDecision
                {
                    IsRewrite = true,
                    RewriteIp = rule.RewriteIp
                },
                _ => DnsDecision.Allow
            };
        }

        return DnsDecision.Allow;
    }

    public void ReloadRulesPublic() => ReloadRules();
}
