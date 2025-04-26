using DnsProxy.Data;
using DnsProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Servers;
public class IndexModel(AppDbContext db) : PageModel
{
    public List<DnsServerEntry> Items { get; private set; } = [];

    public void OnGet() => Items = db.Servers.OrderBy(s => s.Priority).ToList();

    public IActionResult OnPostDelete(int id)
    {
        var ent = db.Servers.Find(id);
        if (ent is null) return NotFound();
        db.Servers.Remove(ent);
        db.SaveChanges();
        return RedirectToPage();
    }
}
