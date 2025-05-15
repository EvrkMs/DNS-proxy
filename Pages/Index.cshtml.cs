using DnsProxy.Data;
using DnsProxy.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages;
public class IndexModel(AppDbContext db) : PageModel
{
    public List<VisitStatistic> Items { get; private set; } = [];

    public void OnGet() =>
        Items = [.. db.Stats
                  .OrderByDescending(s => s.Timestamp)
                  .Take(1000)];
}
