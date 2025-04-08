using System.Net;

namespace DNS_proxy.Core.Interfaces;

public interface IDnsResolver
{
    Task<IPAddress?> ResolveAsync(string domain);
}
