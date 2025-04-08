using DNS_proxy.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DNS_proxy.Data;

public class DnsRulesContext : DbContext
{
    public DbSet<DnsRule> DnsRules => Set<DnsRule>();
    public DbSet<DnsServerEntry> DnsServers => Set<DnsServerEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.CommonApplicationData),
            "DNS Proxy", "rules.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        options.UseSqlite($"Data Source={path}");
    }
}