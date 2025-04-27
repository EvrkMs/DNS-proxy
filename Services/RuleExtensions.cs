using DnsProxy.Models;

namespace DnsProxy.Services
{
    public static class RuleExtensions
    {
        /// Возвращает список серверов, прошедший через Include/Exclude
        // Services/RuleExtensions.cs
        public static async Task<List<DnsServerEntry>> FilterServers(
                this IDnsConfigService cfg,
                string? includeCsv,
                string? excludeCsv,
                int? forceId)
        {
            var list = await cfg.GetAllAsync();

            /* forceId → ровно один апстрим */
            if (forceId is not null)
                return list.Where(s => s.Id == forceId).ToList();

            if (!string.IsNullOrWhiteSpace(includeCsv))
            {
                var white = includeCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(a => a.Trim())
                                      .ToHashSet(StringComparer.OrdinalIgnoreCase);
                list = list.Where(s => white.Contains(s.Address)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(excludeCsv))
            {
                var black = excludeCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(a => a.Trim())
                                      .ToHashSet(StringComparer.OrdinalIgnoreCase);
                list = list.Where(s => !black.Contains(s.Address)).ToList();
            }

            return list;
        }
    }
}