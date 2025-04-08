using DNS_proxy.Core.Interfaces;
using DNS_proxy.Core.Models;
using DNS_proxy.Data;

namespace DNS_proxy.Infrastructure;

public class RuleChecker : IRuleChecker
{
    private static List<DnsRule> _rulesCache = new();
    private static DateTime _lastRulesLoaded = DateTime.MinValue;

    public DnsDecision Check(string clientIp, string domain)
    {
        ReloadIfExpired();

        foreach (var rule in _rulesCache)
        {
            if (!string.IsNullOrWhiteSpace(rule.SourceIp) && rule.SourceIp != clientIp)
                continue;

            if (rule.DomainPattern.StartsWith("*."))
            {
                // Блокирует все поддомены: *.faceit.com → matches x.faceit.com, но не faceit.com
                string bareDomain = rule.DomainPattern.Substring(2);
                if (!domain.EndsWith("." + bareDomain, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else
            {
                // Точное совпадение: faceit.com → только faceit.com
                if (!domain.Equals(rule.DomainPattern, StringComparison.OrdinalIgnoreCase))
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

    private void ReloadIfExpired()
    {
        if ((DateTime.UtcNow - _lastRulesLoaded).TotalSeconds < 60)
            return;

        using var db = new DnsRulesContext();
        _rulesCache = db.DnsRules.ToList();
        _lastRulesLoaded = DateTime.UtcNow;
        Console.WriteLine($"[RuleChecker] Загружено правил: {_rulesCache.Count}");
    }
}
