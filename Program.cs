using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DNS_proxy;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("-service"))
        {
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<DnsProxyBackgroundService>();
                })
                .Build()
                .Run();
        }
        else
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new AppContext());
        }
    }
}
