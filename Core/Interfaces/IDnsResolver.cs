using System.Net;

namespace DNS_proxy.Core.Interfaces;

public interface IResolverService
{
    Task<IPAddress?> ResolveAsync(string domain);
}