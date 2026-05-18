// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace CIMD.IdentityServer;

/// <summary>
/// Defines a policy applied to Client-Initiated Metadata Discovery (CIMD).
/// Implement this interface and register it in DI to control which clients may
/// use CIMD and how their metadata documents are interpreted.
/// </summary>
public interface ICimdPolicy
{
    /// <summary>
    /// Called before the CIMD document is fetched, allowing the policy to
    /// approve or deny the request based on the client URI (and therefore its
    /// domain). Use this to implement allow- or deny-lists for domains.
    /// </summary>
    Task<CimdPolicyResult> CheckDomainAsync(Uri clientUri, CancellationToken ct);

    /// <summary>
    /// Called after the CIMD document has been fetched and its basic
    /// structural validity verified, but before it is mapped to an
    /// IdentityServer <c>Client</c>. The <see cref="CimdRequestContext"/>
    /// provides the parsed document along with the HTTP response headers
    /// from the fetch, enabling policies to inspect Content-Type,
    /// Cache-Control, or custom headers when making validation decisions.
    /// </summary>
    Task<CimdPolicyResult> ValidateDocumentAsync(CimdRequestContext context, CancellationToken ct);

    /// <summary>
    /// Scopes that are always added to every CIMD client, regardless of what
    /// the metadata document requests. These are merged into the final scope
    /// set unconditionally.
    /// </summary>
    IReadOnlyCollection<string> DefaultScopes { get; }

    /// <summary>
    /// Additional scopes (beyond <see cref="DefaultScopes"/>) that a CIMD
    /// client is permitted to request. Any scope in the metadata document
    /// that is not in <see cref="DefaultScopes"/> or
    /// <see cref="AllowedScopes"/> will be silently removed.
    /// If empty, only <see cref="DefaultScopes"/> are permitted.
    /// </summary>
    IReadOnlyCollection<string> AllowedScopes { get; }

    /// Called for each redirect URI declared in the CIMD document, allowing
    /// the policy to approve or deny individual redirect URIs. Per
    /// <see href="https://www.ietf.org/archive/id/draft-ietf-oauth-client-id-metadata-document-01.html#section-6.1">
    /// CIMD spec section 6.1</see>, an authorization server MAY impose
    /// restrictions on <c>redirect_uris</c>, for example restricting them to
    /// the same origin as the <c>client_id</c>.
    /// </summary>
    Task<CimdPolicyResult> ValidateRedirectUriAsync(Uri redirectUri, CimdRequestContext context, CancellationToken ct);
}

/// <summary>
/// The result of a CIMD policy check.
/// </summary>
public sealed class CimdPolicyResult
{
    /// <summary>Gets a result that permits the operation.</summary>
    public static readonly CimdPolicyResult Allow = new(true, null);

    /// <summary>
    /// Returns a result that denies the operation with the given reason.
    /// </summary>
    public static CimdPolicyResult Deny(string reason) => new(false, reason);

    private CimdPolicyResult(bool isAllowed, string? reason)
    {
        IsAllowed = isAllowed;
        Reason = reason;
    }

    /// <summary>Whether the operation is allowed.</summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// Human-readable explanation for a denial; <c>null</c> when allowed.
    /// </summary>
    public string? Reason { get; }
}
