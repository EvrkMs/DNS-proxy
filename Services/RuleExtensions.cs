using DnsProxy.Models;

namespace DnsProxy.Services
{
    public static class RuleExtensions
    {
        /// Возвращает список серверов, прошедший через Include/Exclude
        public static async Task<List<DnsServerEntry>> FilterServers(
            this IDnsConfigService cfg,
            string includeCsv,
            string excludeCsv)
        {
            var all = await cfg.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(includeCsv))
            {
                var white = includeCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .ToHashSet(StringComparer.OrdinalIgnoreCase);

                all = all.Where(s => white.Contains(s.Address)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(excludeCsv))
            {
                var black = excludeCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .ToHashSet(StringComparer.OrdinalIgnoreCase);

                all = all.Where(s => !black.Contains(s.Address)).ToList();
            }

            return all;
        }
    }
}