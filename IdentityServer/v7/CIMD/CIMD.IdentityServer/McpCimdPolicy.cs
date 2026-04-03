// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace CIMD.IdentityServer;

/// <summary>
/// CIMD policy that ensures all CIMD clients receive the scopes needed to
/// access our protected resources. CIMD documents (e.g., VS Code's) typically
/// don't declare any scopes, so this policy adds them server-side.
/// </summary>
internal sealed class McpCimdPolicy : ICimdPolicy
{
    // Scopes that all CIMD clients are always granted, regardless of what
    // the metadata document declares.
    public IReadOnlyCollection<string> DefaultScopes { get; } = ["openid", "profile", "mcp"];

    // Additional scopes that CIMD clients are permitted to request.
    // offline_access is not a default but clients may opt in to it.
    public IReadOnlyCollection<string> AllowedScopes { get; } = ["offline_access"];

    public Task<CimdPolicyResult> CheckDomainAsync(Uri clientUri, CancellationToken ct) =>
        Task.FromResult(CimdPolicyResult.Allow);

    public Task<CimdPolicyResult> ValidateDocumentAsync(
        CimdRequestContext context,
        CancellationToken ct) =>
        Task.FromResult(CimdPolicyResult.Allow);

    public Task<CimdPolicyResult> ValidateRedirectUriAsync(Uri redirectUri, CimdRequestContext context, CancellationToken ct) =>
        // Per CIMD section 6.1, the authorization server MAY restrict
        // redirect URIs to the same origin as the client_id. This sample allows
        // all redirect URIs so that loopback-based clients (e.g., VS Code) work
        // out of the box. To enable same-origin enforcement, uncomment below:
        //
        // var clientUri = context.ClientUri;
        // var sameOrigin = string.Equals(redirectUri.Scheme, clientUri.Scheme, StringComparison.OrdinalIgnoreCase)
        //     && string.Equals(redirectUri.Host, clientUri.Host, StringComparison.OrdinalIgnoreCase)
        //     && redirectUri.Port == clientUri.Port;
        //
        // if (!sameOrigin)
        // {
        //     return Task.FromResult(
        //         CimdPolicyResult.Deny($"Redirect URI '{redirectUri}' does not share the same origin as the client_id '{clientUri}'."));
        // }

        Task.FromResult(CimdPolicyResult.Allow);
}
