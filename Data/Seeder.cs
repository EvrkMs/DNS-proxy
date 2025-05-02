using DnsProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace DnsProxy.Data;

public static class Seeder
{
    public async static void Seed(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        if (!db.Servers.Any())
        {
            db.Servers.AddRange(
                new DnsServerEntry { Address = "https://dns.comss.one/dns-query", Protocol = DnsProtocol.DoH_Wire, Priority = 10 },
                new DnsServerEntry { Address = "https://cloudflare-dns.com/dns-query", Protocol = DnsProtocol.DoH_Json, Priority = 20 },
                new DnsServerEntry { Address = "8.8.8.8", Protocol = DnsProtocol.Udp, Priority = 30 }
            );
            await db.SaveChangesAsync();
        }
        if(!db.ConfigDns.Any())
        {
            db.ConfigDns.Add(new DnsConfig { Strategy = ResolveStrategy.FirstSuccess });
            await db.SaveChangesAsync();
        }
    }
}
