using DNS_proxy.Core.Models;
using DNS_proxy.Data;

namespace DNS_proxy.Service;

public interface IDnsConfigService
{
    List<DnsServerEntry> GetAll();
    void Add(DnsServerEntry entry);
    void Update(DnsServerEntry entry);
    void Delete(int id);
    List<DnsServerEntry> GetOrderedServers(); // по приоритету
}

public class DnsConfigService : IDnsConfigService
{
    public List<DnsServerEntry> GetAll()
    {
        using var db = new DnsRulesContext();
        return [.. db.DnsServers.OrderBy(x => x.Priority)];
    }

    public void Add(DnsServerEntry entry)
    {
        using var db = new DnsRulesContext();
        db.DnsServers.Add(entry);
        db.SaveChanges();
    }

    public void Update(DnsServerEntry entry)
    {
        using var db = new DnsRulesContext();
        db.DnsServers.Update(entry);
        db.SaveChanges();
    }

    public void Delete(int id)
    {
        using var db = new DnsRulesContext();
        var existing = db.DnsServers.Find(id);
        if (existing != null)
        {
            db.DnsServers.Remove(existing);
            db.SaveChanges();
        }
    }

    public List<DnsServerEntry> GetOrderedServers()
    {
        return GetAll();
    }
}
