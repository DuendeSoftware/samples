using Duende.IdentityModel.Client;

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
    /// IdentityServer <c>Client</c>. Use this to validate or restrict
    /// sensitive fields such as requested scopes or grant types.
    /// </summary>
    Task<CimdPolicyResult> ValidateDocumentAsync(Uri clientUri, DynamicClientRegistrationDocument document, CancellationToken ct);
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
