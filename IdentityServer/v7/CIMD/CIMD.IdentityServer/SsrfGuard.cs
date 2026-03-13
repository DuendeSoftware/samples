using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace CIMD.IdentityServer;

/// <summary>
/// Validates that URIs do not resolve to special-use IP addresses (RFC 6890)
/// to prevent Server-Side Request Forgery (SSRF) attacks.
/// Per CIMD spec section 6.5.
/// </summary>
public class SsrfGuard(IServer server)
{
    // RFC 6890 special-use address ranges
    private static readonly IPNetwork[] SpecialUseNetworks =
    [
        // IPv4 (RFC 6890 section 2.2.2)
        IPNetwork.Parse("0.0.0.0/8"),          // "This host on this network"
        IPNetwork.Parse("10.0.0.0/8"),         // Private-Use
        IPNetwork.Parse("100.64.0.0/10"),      // Shared Address Space
        IPNetwork.Parse("127.0.0.0/8"),        // Loopback
        IPNetwork.Parse("169.254.0.0/16"),     // Link Local
        IPNetwork.Parse("172.16.0.0/12"),      // Private-Use
        IPNetwork.Parse("192.0.0.0/24"),       // IETF Protocol Assignments
        IPNetwork.Parse("192.0.2.0/24"),       // Documentation (TEST-NET-1)
        IPNetwork.Parse("192.168.0.0/16"),     // Private-Use
        IPNetwork.Parse("198.18.0.0/15"),      // Benchmarking
        IPNetwork.Parse("198.51.100.0/24"),    // Documentation (TEST-NET-2)
        IPNetwork.Parse("203.0.113.0/24"),     // Documentation (TEST-NET-3)
        IPNetwork.Parse("240.0.0.0/4"),        // Reserved for Future Use
        IPNetwork.Parse("255.255.255.255/32"), // Limited Broadcast

        // IPv6 (RFC 6890 section 2.2.3)
        IPNetwork.Parse("::1/128"),            // Loopback
        IPNetwork.Parse("::/128"),             // Unspecified
        IPNetwork.Parse("::ffff:0:0/96"),      // IPv4-mapped
        IPNetwork.Parse("64:ff9b::/96"),       // IPv4-IPv6 Translation
        IPNetwork.Parse("100::/64"),           // Discard-Only
        IPNetwork.Parse("2001::/23"),          // IETF Protocol Assignments
        IPNetwork.Parse("fc00::/7"),           // Unique-Local
        IPNetwork.Parse("fe80::/10"),          // Link-Scoped Unicast
    ];

    /// <summary>
    /// Returns true if the URI resolves to a safe (non-special-use) address,
    /// with a loopback carve-out when the server itself is on loopback.
    /// </summary>
    public async Task<bool> IsSafeAsync(Uri uri, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, ct);
        return !addresses.Any(a => IsSpecialUseAddress(a) && !(IsLoopback(a) && ServerIsOnLoopback()));
    }

    private static bool IsSpecialUseAddress(IPAddress address) =>
        SpecialUseNetworks.Any(network => network.Contains(address));

    private static bool IsLoopback(IPAddress address) =>
        IPAddress.IsLoopback(address);

    /// <summary>
    /// Per CIMD spec section 6.5: the loopback carve-out applies only when
    /// the server itself is also running on a loopback address.
    /// </summary>
    private bool ServerIsOnLoopback()
    {
        var serverAddresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
            ?? [];
        foreach (var a in serverAddresses)
        {
            if (!Uri.TryCreate(a, UriKind.Absolute, out var uri))
            {
                continue;
            }
            if (IPAddress.TryParse(uri.Host, out var ip))
            {
                if (IPAddress.IsLoopback(ip))
                {
                    return true;
                }
            }
            else if (Dns.GetHostAddresses(uri.Host).Any(IPAddress.IsLoopback))
            {
                return true;
            }
        }
        return false;
    }
}
