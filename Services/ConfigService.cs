using DnsProxy.Data;
using Microsoft.EntityFrameworkCore;

namespace DnsProxy.Services
{
    public interface IConfigService
    {
        Task<Models.DnsConfig> GetConfigAsync(CancellationToken token = default);
        Task SaveStrategyConfigAsync(Models.ResolveStrategy strategy, CancellationToken token = default);
    }

    public class ConfigService(AppDbContext db, ILogger<ConfigService> log) : IConfigService
    {
        public async Task<Models.DnsConfig> GetConfigAsync(CancellationToken token = default)
            => await db.ConfigDns.AsNoTracking().FirstOrDefaultAsync(cancellationToken: token);

        public async Task SaveStrategyConfigAsync(Models.ResolveStrategy strategy, CancellationToken token = default)
        {
            try
            {
                var config = await db.ConfigDns.FirstOrDefaultAsync(cancellationToken: token);
                if (config is null)
                {
                    log.LogWarning("Конфиг DNS не найден в базе данных.");
                    return;
                }

                config.Strategy = strategy;
                await db.SaveChangesAsync(token);
                log.LogInformation("Стратегия успешно сохранена: {Strategy}", strategy);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Ошибка при сохранении стратегии конфигурации");
            }
        }
    }
}
