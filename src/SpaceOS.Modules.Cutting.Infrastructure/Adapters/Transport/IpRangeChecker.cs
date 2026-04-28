using System.Net;
using System.Net.Sockets;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

/// <summary>
/// Checks whether an IP address falls within a blocked private or link-local range (SSRF protection).
/// Blocked: 10/8, 172.16/12, 192.168/16, 169.254/16, 127/8, IPv6 link-local (fe80::/10).
/// </summary>
public interface IIpRangeChecker
{
    /// <summary>Returns true when the address is in a blocked range.</summary>
    bool IsBlocked(IPAddress address);
}

/// <inheritdoc cref="IIpRangeChecker"/>
internal sealed class IpRangeChecker : IIpRangeChecker
{
    public bool IsBlocked(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Normalize IPv4-mapped IPv6
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsBlockedIPv4(address),
            AddressFamily.InterNetworkV6 => IsBlockedIPv6(address),
            _ => false
        };
    }

    private static bool IsBlockedIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return
            // 127.0.0.0/8 — loopback
            bytes[0] == 127 ||
            // 10.0.0.0/8 — private class A
            bytes[0] == 10 ||
            // 172.16.0.0/12 — private class B
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
            // 192.168.0.0/16 — private class C
            (bytes[0] == 192 && bytes[1] == 168) ||
            // 169.254.0.0/16 — link-local / APIPA / cloud metadata
            (bytes[0] == 169 && bytes[1] == 254);
    }

    private static bool IsBlockedIPv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        // fe80::/10 — link-local
        return bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80;
    }
}
