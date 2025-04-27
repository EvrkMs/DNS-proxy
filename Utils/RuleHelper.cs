using DnsProxy.Models;

namespace DnsProxy.Utils;

/// <summary>Поиск первого подходящего правила</summary>
public static class RuleHelper
{
    /// <returns>(action, rewriteIp, includeCsv, excludeCsv)</returns>
    public static (RuleAction Action,
                   string? RewriteIp,
                   string? IncludeCsv,
                   string? ExcludeCsv,
                   int? ForceServer)
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
                    r.ExcludeServers,
                    r.ForceServer?.Id);
        }

        // ничего не подошло — Allow по умолчанию
        return (RuleAction.Allow, null, null, null, null);
    }

    /* ---------------------------------------- */

    // Utils/RuleHelper.cs
    private static bool DomainMatch(string host, string csvPatterns)
    {
        foreach (var p in csvPatterns.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var pat = p.Trim();
            if (pat.Length == 0) continue;

            if (pat.StartsWith("*.", StringComparison.Ordinal))
            {
                var bare = pat[2..];
                if (host.Equals(bare, StringComparison.OrdinalIgnoreCase) ||
                    host.EndsWith('.' + bare, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (host.Equals(pat, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
