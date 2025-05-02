using DnsProxy.Data;
using DnsProxy.Models;
using DnsProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Servers;
public class IndexModel(AppDbContext db, IConfigService config) : PageModel
{
    public List<DnsServerEntry> Items { get; private set; } = [];

    public bool Parallel { get; private set; }  // флаг для галочки

    public async Task OnGetAsync()
    {
        Items = db.Servers.OrderBy(s => s.Priority).ToList();
        var conf = await config.GetConfigAsync();
        Parallel = conf?.Strategy == ResolveStrategy.ParallelAll;
    }

    public IActionResult OnPostDelete(int id)
    {
        var ent = db.Servers.Find(id);
        if (ent is null) return NotFound();
        db.Servers.Remove(ent);
        db.SaveChanges();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetStrategy(bool parallel)
    {
        var conf = await config.GetConfigAsync();
        if (conf != null)
        {
            conf.Strategy = parallel
                ? ResolveStrategy.ParallelAll
                : ResolveStrategy.FirstSuccess;

            await config.SaveStrategyConfigAsync(conf.Strategy);
        }

        return RedirectToPage();
    }
}
