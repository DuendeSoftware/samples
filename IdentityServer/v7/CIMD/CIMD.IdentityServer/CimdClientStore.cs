using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace CIMD.IdentityServer;

/// <summary>
/// Custom <see cref="IClientStore"/> that dynamically creates clients by
/// fetching and validating Client ID Metadata Documents (CIMD).
/// </summary>
public partial class CimdClientStore(
    CimdDocumentFetcher fetcher,
    SsrfGuard ssrfGuard,
    IEnumerable<Client> staticClients,
    ICimdPolicy policy,
    ILogger<CimdClientStore> logger) : IClientStore
{
    private readonly List<Client> _clients = new(staticClients);

    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        // Check cache first
        var existing = _clients.FirstOrDefault(c => c.ClientId == clientId);
        if (existing != null)
        {
            Log.FoundCachedClient(logger, clientId);
            return existing;
        }

        // Validate the client_id is a well-formed CIMD URI
        if (!CimdDocumentValidator.TryParseClientUri(clientId, out var clientUri))
        {
            Log.InvalidClientUri(logger, clientId);
            return null;
        }

        // The IClientStore interface doesn't provide a CancellationToken, so we
        // create our own with a timeout to bound all outgoing network calls
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var ct = cts.Token;

        // SSRF protection (CIMD spec section 6.5)
        if (!await ssrfGuard.IsSafeAsync(clientUri, ct))
        {
            Log.SsrfCheckFailed(logger, clientId);
            return null;
        }

        // Domain allowlist policy
        var domainResult = await policy.CheckDomainAsync(clientUri, ct);
        if (!domainResult.IsAllowed)
        {
            Log.DomainDeniedByPolicy(logger, clientId, domainResult.Reason);
            return null;
        }

        // Fetch and deserialize the CIMD document
        var document = await fetcher.FetchDocumentAsync(clientUri, ct);
        if (document == null)
        {
            return null;
        }

        // Validate document contents
        if (!CimdDocumentValidator.ClientIdMatchesDocument(clientId, document))
        {
            Log.ClientIdMismatch(logger, clientId);
            return null;
        }

        if (!CimdDocumentValidator.PassesAuthMethodChecks(document, out var authMethodFailureReason))
        {
            Log.AuthMethodCheckFailed(logger, clientId, authMethodFailureReason);
            return null;
        }

        // Document-level policy check
        var documentResult = await policy.ValidateDocumentAsync(clientUri, document, ct);
        if (!documentResult.IsAllowed)
        {
            Log.DocumentDeniedByPolicy(logger, clientId, documentResult.Reason);
            return null;
        }

        // Resolve keys and build the IdentityServer client
        var keySet = await fetcher.ResolveJwksAsync(document, ct);
        var client = CimdClientBuilder.Build(clientId, document, keySet);

        _clients.Add(client);
        Log.RegisteredCimdClient(logger, clientId);
        return client;
    }
}
