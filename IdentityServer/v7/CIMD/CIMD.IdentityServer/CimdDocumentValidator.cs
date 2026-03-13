using Duende.IdentityModel.Client;

namespace CIMD.IdentityServer;

/// <summary>
/// Pure validation logic for CIMD documents — no dependencies, fully static.
/// </summary>
public static class CimdDocumentValidator
{
    /// <summary>
    /// Per spec section 3: client URI must be HTTPS, contain a path component,
    /// and MUST NOT contain single/double-dot path segments, a fragment, or
    /// a username or password.
    /// </summary>
    public static bool TryParseClientUri(string clientId, out Uri clientUri)
    {
        if (!Uri.TryCreate(clientId, UriKind.Absolute, out clientUri!) ||
            clientUri.Scheme != "https" ||
            string.IsNullOrEmpty(clientUri.AbsolutePath.TrimStart('/')) ||
            !string.IsNullOrEmpty(clientUri.Fragment) ||
            !string.IsNullOrEmpty(clientUri.UserInfo) ||
            clientUri.Segments.Any(s => s == "./" || s == "../"))
        {
            clientUri = null!;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Per spec section 4.1: client_id property MUST match the URL using
    /// simple string comparison.
    /// </summary>
    public static bool ClientIdMatchesDocument(
        string clientId, DynamicClientRegistrationDocument document) =>
        document.Extensions.TryGetValue("client_id", out var clientIdElement) &&
        clientIdElement.GetString() == clientId;

    /// <summary>
    /// Per spec section 4.1: token_endpoint_auth_method MUST NOT be a
    /// shared-secret method, and client_secret / client_secret_expires_at
    /// MUST NOT be present.
    /// </summary>
    public static bool PassesAuthMethodChecks(
        DynamicClientRegistrationDocument document, out string failureReason)
    {
        if (document.TokenEndpointAuthenticationMethod is { } method &&
            BannedAuthMethods.Contains(method))
        {
            failureReason = $"token_endpoint_auth_method '{method}' is a shared-secret method, which is not permitted in CIMD documents";
            return false;
        }

        if (document.Extensions.ContainsKey("client_secret"))
        {
            failureReason = "client_secret is not permitted in CIMD documents";
            return false;
        }

        if (document.Extensions.ContainsKey("client_secret_expires_at"))
        {
            failureReason = "client_secret_expires_at is not permitted in CIMD documents";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private static readonly HashSet<string> BannedAuthMethods =
    [
        "client_secret_post",
        "client_secret_basic",
        "client_secret_jwt",
    ];
}
