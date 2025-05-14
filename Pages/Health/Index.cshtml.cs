// Pages/Health/Index.cshtml.cs
using System.Diagnostics;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Models;
using DnsProxy.Services;
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
                      long Ms,
                      string Type);

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

        string proto = s.Protocol switch
        {
            DnsProtocol.Udp => "UDP",
            DnsProtocol.DoH_Wire => "DoH-wire",
            DnsProtocol.DoH_Json => "DoH-json",
            _ => "Unknown"
        };

        try
        {
            var result = await resolver.ResolveAsync(domain, RecordType.A, single);
            sw.Stop();

            return new Row(
                Server: s.Address,
                Proto: proto,
                Rcode: result.RCode,
                Ip: result.Records.OfType<ARecord>().FirstOrDefault()?.Address.ToString() ?? "-",
                Ttl: result.Ttl,
                Ms: sw.ElapsedMilliseconds,
                Type: result.Records.FirstOrDefault()?.RecordType.ToString() ?? "Unknown"
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new Row(
                Server: s.Address,
                Proto: proto,
                Rcode: ex.GetBaseException().Message,
                Ip: "-",
                Ttl: 0,
                Ms: sw.ElapsedMilliseconds,
                Type: "Error"
            );
        }
    }
}
