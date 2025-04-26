// Pages/Health/Index.cshtml.cs
using System.Diagnostics;
using DnsProxy.Models;
using DnsProxy.Services;
using DnsProxy.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DnsProxy.Pages.Health;
public class IndexModel(IDnsConfigService cfg,
                        IResolverService resolver) : PageModel
{
    public record Row(string Server,
                      string Proto,
                      string Rcode,
                      string Ip,
                      int Ttl,
                      long Ms);

    public List<Row> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        const string domain = "chatgpt.com";
        var servers = await cfg.GetAllAsync();

        var tasks = servers.Select(s => ProbeAsync(s, domain, resolver));
        Items = (await Task.WhenAll(tasks)).ToList();
    }

    /* ---------- один запрос к апстриму ---------- */
    private static async Task<Row> ProbeAsync(DnsServerEntry s, string domain, IResolverService resolver)
    {
        var sw = Stopwatch.StartNew();
        var single = new List<DnsServerEntry> { s };
        try
        {
            // пытаемся резолвить через единственный сервер
            var (ip, ttl, upstream, diag) = await resolver.ResolveAsync(domain, single);
            sw.Stop();

            // строим название протокола по enum
            string proto = s.Protocol switch
            {
                DnsProtocol.Udp => "UDP",
                DnsProtocol.DoH_Wire => "DoH-wire",
                DnsProtocol.DoH_Json => "DoH-json",
                _ => "Unknown"
            };

            return new Row(
                Server: s.Address,
                Proto: proto,
                Rcode: diag,                          // сюда попадает "NOERROR", "NXDOMAIN" или текст исключения
                Ip: ip?.ToString() ?? "-",
                Ttl: ip is null ? 0 : ttl,
                Ms: sw.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            string proto = s.Protocol switch
            {
                DnsProtocol.Udp => "UDP",
                DnsProtocol.DoH_Wire => "DoH-wire",
                DnsProtocol.DoH_Json => "DoH-json",
                _ => "Unknown"
            };
            return new Row(
                Server: s.Address,
                Proto: proto,
                Rcode: ex.GetBaseException().Message,  // детальный текст ошибки
                Ip: "-",
                Ttl: 0,
                Ms: sw.ElapsedMilliseconds
            );
        }
    }
}
