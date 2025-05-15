using System.Net;
using ARSoft.Tools.Net.Dns;

namespace DnsProxy.Models;

public enum RuleAction { Allow, Block, Rewrite }

public class DnsRule
{
    public int Id { get; set; }
    public string SourceIp { get; set; } = "*";
    public string DomainPattern { get; set; } = "";

    public RuleAction Action { get; set; }

    public string? RewriteIp { get; set; }

    /* FK  ⇄  навигация */
    public int? ForceServerId { get; set; }
    public DnsServerEntry? ForceServer { get; set; }

    public string? IncludeServers { get; set; }
    public string? ExcludeServers { get; set; }
}

public enum DnsProtocol
{
    Udp,
    DoH_Wire,     // wire-формат (RFC 8484)
    DoH_Json,     // JSON-формат (Google/Cloudflare style)
}
public class DnsServerEntry
{
    public int Id { get; set; }
    public string? Address { get; set; }
    public DnsProtocol Protocol { get; set; }
    public int Priority { get; set; }
    public IPAddress? StaticAddress { get; set; }
}

public class VisitStatistic
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ClientIp { get; set; } = "";
    public string? Domain { get; set; }
    public string? Upstream { get; set; }

    public string Rcode { get; set; } = "NOERROR";   // NEW («NOERROR», «NXDOMAIN», «TIMEOUT»…)
    public RecordType Type { get; set; } = RecordType.Invalid;
    public RuleAction Action { get; set; } = RuleAction.Block;
}
public enum ResolveStrategy
{
    FirstSuccess,  // классика, как сейчас
    ParallelAll    // параллельно, берём первый успешный
}
public class DnsConfig
{
    public int Id { get; set; }
    public ResolveStrategy Strategy { get; set; } = ResolveStrategy.FirstSuccess;

}