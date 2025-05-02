using System;
using DnsProxy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DnsProxy.Services
{
    public interface IConfigService
    {
        Task<Models.DnsConfig> GetConfigAsync();
        Task SaveStrategyConfigAsync(Models.ResolveStrategy strategy);
    }

    public class ConfigService(AppDbContext db, ILogger<ConfigService> log) : IConfigService
    {
        public async Task<Models.DnsConfig> GetConfigAsync()
            => await db.ConfigDns.AsNoTracking().FirstOrDefaultAsync();

        public async Task SaveStrategyConfigAsync(Models.ResolveStrategy strategy)
        {
            try
            {
                var config = await db.ConfigDns.FirstOrDefaultAsync();
                if (config is null)
                {
                    log.LogWarning("Конфиг DNS не найден в базе данных.");
                    return;
                }

                config.Strategy = strategy;
                await db.SaveChangesAsync();
                log.LogInformation("Стратегия успешно сохранена: {Strategy}", strategy);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Ошибка при сохранении стратегии конфигурации");
            }
        }
    }
}
