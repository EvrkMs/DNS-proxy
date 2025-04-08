using DNS_proxy.Service;
using Microsoft.Extensions.Hosting;
using static DNS_proxy.Utils.Utils;

namespace DNS_proxy;

public class DnsProxyBackgroundService : BackgroundService
{
    private readonly CustomDnsServer _dnsServer = DnsServerInstance.Server;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        MigrateAndSeed();
        _dnsServer.Start();
        Console.WriteLine("DNS-сервис запущен как служба Windows");
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Остановка службы...");
        _dnsServer.Stop(); // нужно реализовать Stop()
        return base.StopAsync(cancellationToken);
    }
}

