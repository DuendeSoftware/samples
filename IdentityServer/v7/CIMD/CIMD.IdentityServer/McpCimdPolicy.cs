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
    // Scopes that all CIMD clients should be granted. The authorization server
    // owns this list — the client doesn't need to know about them upfront.
    private static readonly string[] DefaultScopes = ["openid", "profile", "mcp"];

    public Task<CimdPolicyResult> CheckDomainAsync(Uri clientUri, CancellationToken ct) =>
        Task.FromResult(CimdPolicyResult.Allow);

    public Task<CimdPolicyResult> ValidateDocumentAsync(
        CimdRequestContext context,
        CancellationToken ct)
    {
        // Merge default scopes into whatever the document already declares
        var existing = context.Document.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var merged = existing.Union(DefaultScopes, StringComparer.OrdinalIgnoreCase);
        context.Document.Scope = string.Join(' ', merged);

        return Task.FromResult(CimdPolicyResult.Allow);
    }
}
