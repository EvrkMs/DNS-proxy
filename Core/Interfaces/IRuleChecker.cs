using DNS_proxy.Core.Models;

namespace DNS_proxy.Core.Interfaces;

public interface IRuleChecker
{
    DnsDecision Check(string clientIp, string domain);
}
