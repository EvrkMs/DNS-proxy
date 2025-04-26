
using DnsProxy.Models;
using Microsoft.EntityFrameworkCore;

namespace DnsProxy.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DnsRule> Rules => Set<DnsRule>();
    public DbSet<DnsServerEntry> Servers => Set<DnsServerEntry>();
    public DbSet<VisitStatistic> Stats => Set<VisitStatistic>();
}
