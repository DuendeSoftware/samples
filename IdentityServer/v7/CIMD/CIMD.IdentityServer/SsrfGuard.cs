// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using idunno.Security;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace CIMD.IdentityServer;

/// <summary>
/// Validates that URIs do not resolve to special-use IP addresses (RFC 6890)
/// to prevent Server-Side Request Forgery (SSRF) attacks.
///
/// This is Layer 1 of a two-layer SSRF protection strategy:
///   Layer 1 (this class): Pre-flight check on the client_id URL with a
///   loopback carve-out per CIMD spec section 6.5.
///   Layer 2: Connection-time SSRF protection via idunno.Security.Ssrf's
///   HTTP handler (see <see cref="HostingExtensions"/>).
///
/// IP range checking is delegated to <see cref="Ssrf.IsUnsafeIpAddress"/>
/// from idunno.Security.Ssrf.
/// </summary>
public class SsrfGuard(IServer server, IHostEnvironment environment)
{
    /// <summary>
    /// Returns true if the URI resolves to a safe (non-special-use) address,
    /// with a loopback carve-out when the server itself is on loopback.
    /// </summary>
    public async Task<bool> IsSafeAsync(Uri uri, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, ct);
        var allLoopback = addresses.All(IPAddress.IsLoopback);

        // Loopback carve-out per CIMD spec section 6.5:
        // Allow loopback when running in development and the server itself is on loopback.
        if (environment.IsDevelopment() && allLoopback && ServerIsOnLoopback())
        {
            return true;
        }

        // Delegate RFC 6890 checking to idunno.Security.Ssrf.
        return !addresses.Any(Ssrf.IsUnsafeIpAddress);
    }

    /// <summary>
    /// Per CIMD spec section 6.5: the loopback carve-out applies only when
    /// the server itself is also running on a loopback address. This method
    /// is also used by the HTTP handler registration in
    /// <see cref="HostingExtensions"/> to decide whether to enable
    /// connection-time SSRF protection.
    /// </summary>
    public bool ServerIsOnLoopback()
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
