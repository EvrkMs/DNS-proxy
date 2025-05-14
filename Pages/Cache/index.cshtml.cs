using DnsProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ARSoft.Tools.Net.Dns;

namespace DnsProxy.Pages.Cache;

public class CacheViewerModel(ICacheService cache) : PageModel
{
    public record Entry(string Key, string Ip, int Ttl);

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public List<Entry> Entries { get; private set; } = [];

    public void OnGet()
    {
        var all = cache.GetAllEntries()
            .SelectMany(e => e.Records
                .OfType<ARecord>() // или добавить AaaaRecord, если хочешь
                .Select(r => new Entry(e.Key, r.Address.ToString(), e.Ttl)));

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var q = Search.ToLowerInvariant();
            Entries = [.. all
                .Where(e => e.Key.Contains(q, StringComparison.InvariantCultureIgnoreCase)
                         || e.Ip.Contains(q, StringComparison.InvariantCultureIgnoreCase))];
        }
        else
        {
            Entries = [.. all];
        }
    }
}
