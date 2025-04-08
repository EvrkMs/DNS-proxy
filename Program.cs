using DNS_proxy.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DNS_proxy;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("-service"))
        {
            Utils.Utils.MigrateAndSeed();

            var configService = new DnsConfigService();
            var ruleService = new RuleService();
            var resolverService = new ResolverService();

            var dnsServer = new CustomDnsServer(configService, ruleService, resolverService);
            var service = new DnsProxyBackgroundService(dnsServer);

            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices(services =>
                {
                    services.AddHostedService(_ => service);
                })
                .Build()
                .Run();
        }
        else
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Utils.Utils.MigrateAndSeed();

            // Всё создаём руками
            var configService = new DnsConfigService();
            var ruleService = new RuleService();
            var resolverService = new ResolverService();

            var dnsServer = new CustomDnsServer(configService, ruleService, resolverService);

            // UI запускаем с явно переданными зависимостями
            Application.Run(new AppContext(dnsServer));
        }
    }
}
