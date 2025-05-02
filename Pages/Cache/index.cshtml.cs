using DnsProxy.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Cache;

public class CacheViewerModel(ICacheService cache) : PageModel
{
    public List<(string Key, string Ip, int Ttl)> Entries { get; private set; } = [];

    public void OnGet()
    {
        Entries = [.. cache
            .GetAllEntries()
            .Select(e => (e.Key, e.Ip.ToString(), e.Ttl))];
    }
}
