using DNS_proxy.Service;
using Microsoft.Extensions.Hosting;
using static DNS_proxy.Utils.Utils;

namespace DNS_proxy;

public class DnsProxyBackgroundService(CustomDnsServer dnsServer) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        MigrateAndSeed();
        dnsServer.Start();
        Console.WriteLine("DNS-сервис запущен как служба Windows");
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Остановка службы...");
        dnsServer.Stop();
        return base.StopAsync(cancellationToken);
    }
}
