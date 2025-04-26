using DnsProxy.Data;
using DnsProxy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Servers;
public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty] public DnsServerEntry Item { get; set; } = new();

    public void OnGet(int? id)
    {
        if (id is not null) Item = db.Servers.Find(id) ?? new();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid) return Page();
        if (Item.Id == 0)
            await db.Servers.AddAsync(Item);
        else
            db.Servers.Update(Item);
        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
