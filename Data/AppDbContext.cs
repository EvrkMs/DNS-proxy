
using DnsProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace DnsProxy.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DnsRule> Rules => Set<DnsRule>();
    public DbSet<DnsServerEntry> Servers => Set<DnsServerEntry>();
    public DbSet<VisitStatistic> Stats => Set<VisitStatistic>();
    public DbSet<DnsConfig> ConfigDns => Set<DnsConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DnsRule>()
            .HasOne(r => r.ForceServer)
            .WithMany() // если у сервера нет коллекции правил
            .HasForeignKey(r => r.ForceServerId)
            .OnDelete(DeleteBehavior.SetNull); // или .Restrict / .Cascade
    }

}
