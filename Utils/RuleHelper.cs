using DnsProxy.Models;

namespace DnsProxy.Utils;

/// <summary>Поиск первого подходящего правила</summary>
public static class RuleHelper
{
    /// <returns>(action, rewriteIp, includeCsv, excludeCsv)</returns>
    public static (RuleAction Action,
                   string? RewriteIp,
                   string? IncludeCsv,
                   string? ExcludeCsv)
        Apply(IEnumerable<DnsRule> rules, string clientIp, string domain)
    {
        foreach (var r in rules)
        {
            // 1. IP-фильтр (пусто или «*» == любой IP)
            if (!string.IsNullOrWhiteSpace(r.SourceIp) &&
                r.SourceIp != "*" &&
                !IpMatchHelper.IsMatch(clientIp, r.SourceIp))
                continue;

            // 2. доменный шаблон
            if (!DomainMatch(domain, r.DomainPattern))
                continue;

            // — нашли правило —
            return (r.Action,
                    r.RewriteIp,
                    r.IncludeServers,
                    r.ExcludeServers);
        }

        // ничего не подошло — Allow по умолчанию
        return (RuleAction.Allow, null, null, null);
    }

    /* ---------------------------------------- */

    private static bool DomainMatch(string host, string pattern)
    {
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            var bare = pattern[2..];
            return host.Equals(bare, StringComparison.OrdinalIgnoreCase) ||
                   host.EndsWith("." + bare, StringComparison.OrdinalIgnoreCase);
        }
        return host.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
