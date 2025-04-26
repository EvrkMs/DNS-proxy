using System.Net;

namespace DnsProxy.Utils;

public static class IpMatchHelper
{
    public static bool IsMatch(string clientIp, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
            return true;
        if (pattern.Contains('/'))
        {
            var parts = pattern.Split('/');
            if (!IPAddress.TryParse(parts[0], out var network)) return false;
            if (!int.TryParse(parts[1], out var prefix)) return false;
            if (!IPAddress.TryParse(clientIp, out var ip)) return false;
            uint net = ToUint(network);
            uint addr = ToUint(ip);
            uint mask = prefix == 0 ? 0 : 0xFFFFFFFFu << (32 - prefix);
            return (net & mask) == (addr & mask);
        }
        return clientIp.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
    private static uint ToUint(IPAddress ip) => BitConverter.ToUInt32(ip.GetAddressBytes().Reverse().ToArray(), 0);
}
