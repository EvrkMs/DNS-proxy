using System.Net;

namespace DNS_proxy.Utils;

public static class IpMatchHelper
{
    public static bool IsIpMatch(string ip, string rule)
    {
        if (rule.Contains('/')) return MatchCidr(ip, rule);
        if (rule.Contains('-')) return MatchRange(ip, rule);
        return ip == rule;
    }

    private static bool MatchRange(string ipStr, string range)
    {
        var parts = range.Split('-');
        if (parts.Length != 2) return false;

        if (!IPAddress.TryParse(ipStr, out var ip)) return false;
        if (!IPAddress.TryParse(parts[0], out var start)) return false;
        if (!IPAddress.TryParse(parts[1], out var end)) return false;

        var ipBytes = ip.GetAddressBytes();
        var startBytes = start.GetAddressBytes();
        var endBytes = end.GetAddressBytes();

        if (ipBytes.Length != startBytes.Length || ipBytes.Length != endBytes.Length)
            return false;

        for (int i = 0; i < ipBytes.Length; i++)
        {
            if (ipBytes[i] < startBytes[i]) return false;
            if (ipBytes[i] > endBytes[i]) return false;
        }

        return true;
    }

    private static bool MatchCidr(string ipStr, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(ipStr, out var ip) || !IPAddress.TryParse(parts[0], out var net))
            return false;

        int prefixLength = int.Parse(parts[1]);

        var ipBytes = ip.GetAddressBytes();
        var netBytes = net.GetAddressBytes();

        if (ipBytes.Length != netBytes.Length)
            return false;

        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
            if (ipBytes[i] != netBytes[i])
                return false;

        if (remainingBits == 0) return true;

        int mask = 0xFF << (8 - remainingBits);
        return (ipBytes[fullBytes] & mask) == (netBytes[fullBytes] & mask);
    }
}
