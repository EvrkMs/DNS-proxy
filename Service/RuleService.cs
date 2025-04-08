using DNS_proxy.Core.Models;
using DNS_proxy.Data;

namespace DNS_proxy.Service;

public interface IRuleService
{
    List<DnsRule> GetAllRules();
    void AddRule(DnsRule rule);
    void UpdateRule(DnsRule rule);
    void DeleteRule(int id);
}

public class RuleService : IRuleService
{
    public List<DnsRule> GetAllRules()
    {
        using var db = new DnsRulesContext();
        return [.. db.DnsRules];
    }

    public void AddRule(DnsRule rule)
    {
        using var db = new DnsRulesContext();
        db.DnsRules.Add(rule);
        db.SaveChanges();
    }

    public void UpdateRule(DnsRule rule)
    {
        using var db = new DnsRulesContext();
        db.DnsRules.Update(rule);
        db.SaveChanges();
    }

    public void DeleteRule(int id)
    {
        using var db = new DnsRulesContext();
        var rule = db.DnsRules.Find(id);
        if (rule != null)
        {
            db.DnsRules.Remove(rule);
            db.SaveChanges();
        }
    }
}
