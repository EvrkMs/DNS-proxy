using DnsProxy.Data;
using DnsProxy.Models;
using DnsProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DnsProxy.Pages.Rules;
public class EditModel(AppDbContext db, IDnsConfigService cfg) : PageModel
{
    private readonly AppDbContext _db = db;

    // ← список для комбобоксов
    public List<SelectListItem> ServerOptions { get; private set; } = [];

    [BindProperty] public DnsRule Item { get; set; } = new();

    public async Task OnGetAsync(int? id)
    {
        if (id is not null)
            Item = _db.Rules.Find(id) ?? new();

        var servers = await cfg.GetAllAsync();
        ServerOptions = servers
            .Select(s => new SelectListItem(s.Address, s.Address))
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(string[] include, string[] exclude, string? force)
    {
        if (!ModelState.IsValid) return Page();

        // приводим формы к CSV или null
        Item.IncludeServers = include.Length == 0 ? null : string.Join(',', include);
        Item.ExcludeServers = exclude.Length == 0 ? null : string.Join(',', exclude);
        Item.RewriteIp = string.IsNullOrWhiteSpace(Item.RewriteIp) ? null : Item.RewriteIp;

        if (!string.IsNullOrEmpty(force))
            Item.IncludeServers = force;   // «форсировать» > любого списка

        if (Item.Id == 0)
            _db.Rules.Add(Item);
        else
            _db.Rules.Update(Item);

        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
