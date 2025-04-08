using System.Net;

namespace DNS_proxy.Core.Models;

public class CacheEntry(IPAddress ip, DateTime deadline)
{
    public IPAddress IP { get; } = ip;
    public DateTime Deadline { get; } = deadline;
}
