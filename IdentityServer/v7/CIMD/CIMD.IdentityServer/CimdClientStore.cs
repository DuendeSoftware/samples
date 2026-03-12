using System.Buffers;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.Jwk;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace CIMD.IdentityServer;

public partial class CimdClientStore(
    IHttpClientFactory httpClientFactory,
    IServer server,
    IEnumerable<Client> staticClients,
    ICimdPolicy policy,
    ILogger<CimdClientStore> logger) : IClientStore
{
    public const string HttpClientName = "cimd";

    private readonly List<Client> _clients = new(staticClients);

    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        // If _clients contains the client id already, just return it
        var existing = _clients.FirstOrDefault(c => c.ClientId == clientId);
        if (existing != null)
        {
            Log.FoundCachedClient(logger, clientId);
            return existing;
        }

        if (!TryParseClientUri(clientId, out var clientUri))
        {
            Log.InvalidClientUri(logger, clientId);
            return null;
        }

        // The IClientStore interface doesn't provide a CancellationToken, so we
        // create our own with a timeout to bound all outgoing network calls
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var ct = cts.Token;

        if (!await PassesSsrfChecksAsync(clientUri, ct))
        {
            Log.SsrfCheckFailed(logger, clientId);
            return null;
        }

        var domainResult = await policy.CheckDomainAsync(clientUri, ct);
        if (!domainResult.IsAllowed)
        {
            Log.DomainDeniedByPolicy(logger, clientId, domainResult.Reason);
            return null;
        }

        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var document = await FetchCimdDocumentAsync(httpClient, clientUri, ct);
        if (document == null)
        {
            return null;
        }

        if (!ClientIdMatchesDocument(clientId, document))
        {
            Log.ClientIdMismatch(logger, clientId);
            return null;
        }

        if (!PassesAuthMethodChecks(document, out var authMethodFailureReason))
        {
            Log.AuthMethodCheckFailed(logger, clientId, authMethodFailureReason);
            return null;
        }

        var documentResult = await policy.ValidateDocumentAsync(clientUri, document, ct);
        if (!documentResult.IsAllowed)
        {
            Log.DocumentDeniedByPolicy(logger, clientId, documentResult.Reason);
            return null;
        }

        var keySet = await ResolveJwksAsync(httpClient, document, ct);

        var client = BuildClient(clientId, document, keySet);

        // Add it to our in-memory list
        _clients.Add(client);

        Log.RegisteredCimdClient(logger, clientId);
        return client;
    }

    // Per spec section 3: client URI must be HTTPS, contain a path component,
    // and MUST NOT contain single/double-dot path segments, a fragment, or
    // a username or password.
    private static bool TryParseClientUri(string clientId, out Uri clientUri)
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

    // Per CIMD spec section 6.5: MUST validate the URL does not resolve to
    // special-use IP addresses as defined in RFC 6890, except loopback when
    // the server itself is also running on a loopback address
    private async Task<bool> PassesSsrfChecksAsync(Uri clientUri, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(clientUri.DnsSafeHost, ct);
        return !addresses.Any(a => IsSpecialUseAddress(a) && !(IsLoopback(a) && ServerIsOnLoopback()));
    }

    // Fetches the CIMD document from the client URI and parses it.
    private const int MaxDocumentSizeBytes = 5 * 1024; // 5 KB per spec section 6.6

    private async Task<DynamicClientRegistrationDocument?> FetchCimdDocumentAsync(
        HttpClient httpClient, Uri clientUri, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(clientUri, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex)
        {
            Log.HttpRequestFailed(logger, clientUri, ex);
            return null;
        }

        // Per spec section 4: MUST treat all non-200 status codes as errors
        if (!response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.OK)
        {
            Log.NonSuccessStatusCode(logger, clientUri, response.StatusCode);
            return null;
        }

        // Per spec section 6.6: SHOULD limit response size to 5 KB.
        // Check Content-Length first to reject obviously oversized responses
        // before reading any body bytes.
        if (response.Content.Headers.ContentLength is > MaxDocumentSizeBytes)
        {
            Log.DocumentTooLarge(logger, clientUri, response.Content.Headers.ContentLength.Value, MaxDocumentSizeBytes);
            return null;
        }

        // Use ArrayPool to avoid allocating a new buffer on every request
        var buffer = ArrayPool<byte>.Shared.Rent(MaxDocumentSizeBytes + 1);
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(ct);
            var bytesRead = await stream.ReadAtLeastAsync(buffer, MaxDocumentSizeBytes + 1, throwOnEndOfStream: false, cancellationToken: ct);

            // Content-Length can be absent or lying — verify actual bytes read
            if (bytesRead > MaxDocumentSizeBytes)
            {
                Log.DocumentTooLarge(logger, clientUri, bytesRead, MaxDocumentSizeBytes);
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<DynamicClientRegistrationDocument>(buffer.AsSpan(0, bytesRead));
            }
            catch (Exception ex)
            {
                Log.DocumentDeserializationFailed(logger, clientUri, ex);
                return null;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // Per spec section 4.1: client_id property MUST match the URL using simple string comparison
    private static bool ClientIdMatchesDocument(string clientId, DynamicClientRegistrationDocument document) =>
        document.Extensions.TryGetValue("client_id", out var clientIdElement) &&
        clientIdElement.GetString() == clientId;

    // Per spec section 4.1: token_endpoint_auth_method MUST NOT be a shared-secret
    // method, and client_secret / client_secret_expires_at MUST NOT be present.
    private static readonly HashSet<string> BannedAuthMethods =
    [
        "client_secret_post",
        "client_secret_basic",
        "client_secret_jwt",
    ];

    private static bool PassesAuthMethodChecks(DynamicClientRegistrationDocument document, out string failureReason)
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

    // Resolves the JWKS: prefers inline jwks, falls back to fetching jwks_uri.
    private async Task<JsonWebKeySet?> ResolveJwksAsync(
        HttpClient httpClient, DynamicClientRegistrationDocument document, CancellationToken ct)
    {
        if (document.Jwks is not null)
        {
            Log.UsingInlineJwks(logger);
            return document.Jwks;
        }

        if (document.JwksUri is not null)
        {
            Log.FetchingJwksUri(logger, document.JwksUri);
            JsonWebKeySetResponse jwksResponse;
            try
            {
                jwksResponse = await httpClient.GetJsonWebKeySetAsync(document.JwksUri.ToString(), ct);
            }
            catch (Exception ex)
            {
                Log.JwksUriFetchFailed(logger, document.JwksUri, ex);
                return null;
            }

            if (!jwksResponse.IsError && jwksResponse.KeySet is not null)
            {
                return jwksResponse.KeySet;
            }

            Log.JwksUriResponseError(logger, document.JwksUri, jwksResponse.Error);
            return null;
        }

        Log.NoJwks(logger);
        return null;
    }

    // Builds an IdentityServer Client from the CIMD document and optional JWKS
    private static Client BuildClient(string clientId, DynamicClientRegistrationDocument document, JsonWebKeySet? keySet)
    {
        var scopes = document.Scope?.Split(' ').ToList() ?? [];
        var allowOfflineAccess = scopes.Contains("offline_access");
        scopes.Remove("offline_access");

        var client = new Client
        {
            ClientId = clientId,
            ClientName = document.ClientName,
            LogoUri = document.LogoUri?.ToString(),
            ClientUri = document.ClientUri?.ToString(),
            RedirectUris = document.RedirectUris?.Select(u => u.ToString()).ToList() ?? [],
            PostLogoutRedirectUris = document.PostLogoutRedirectUris?.Select(u => u.ToString()).ToList() ?? [],
            AllowedGrantTypes = document.GrantTypes?.ToList() ?? GrantTypes.Code,
            RequireClientSecret = keySet is not null,
            AllowedScopes = scopes,
            AllowOfflineAccess = allowOfflineAccess
        };

        if (keySet is not null)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
            };

            foreach (var key in keySet.Keys)
            {
                var jwk = JsonSerializer.Serialize(key, jsonOptions);
                client.ClientSecrets.Add(new Secret
                {
                    Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                    Value = jwk
                });
            }
        }

        return client;
    }

    // RFC 6890 special-use address ranges that must not be fetched (CIMD spec section 6.5)
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

    private static bool IsSpecialUseAddress(IPAddress address) =>
        SpecialUseNetworks.Any(network => network.Contains(address));

    private static bool IsLoopback(IPAddress address) =>
        IPAddress.IsLoopback(address);

    // Per CIMD spec section 6.5: the loopback carve-out applies only when
    // the server itself is also running on a loopback address
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
