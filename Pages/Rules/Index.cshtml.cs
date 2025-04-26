using DnsProxy.Data;
using DnsProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Page.Rules;
public class IndexModel(AppDbContext db) : PageModel
{
    public List<DnsRule> Items { get; private set; } = [];

    public void OnGet() => Items = db.Rules.ToList();

    public IActionResult OnPostDelete(int id)
    {
        var r = db.Rules.Find(id);
        if (r is null) return NotFound();
        db.Rules.Remove(r);
        db.SaveChanges();
        return RedirectToPage();
    }
}