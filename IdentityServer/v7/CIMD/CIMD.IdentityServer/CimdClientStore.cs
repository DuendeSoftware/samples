using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.Caching.Hybrid;

namespace CIMD.IdentityServer;

/// <summary>
/// Custom <see cref="IClientStore"/> that dynamically creates clients by
/// fetching and validating Client ID Metadata Documents (CIMD).
/// Uses <see cref="HybridCache"/> for caching with automatic expiration.
/// </summary>
public partial class CimdClientStore(
    CimdDocumentFetcher fetcher,
    SsrfGuard ssrfGuard,
    ICimdPolicy policy,
    HybridCache cache,
    ILogger<CimdClientStore> logger) : IClientStore
{
    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        try
        {
            return await cache.GetOrCreateAsync(
                $"cimd-client:{clientId}",
                async ct => await ResolveClientAsync(clientId, ct),
                cancellationToken: CancellationToken.None);
        }
        catch (CimdResolutionException)
        {
            // Resolution failed — don't cache the failure
            return null;
        }
    }

    /// <summary>
    /// Performs the full CIMD resolution pipeline. Throws
    /// <see cref="CimdResolutionException"/> on any validation failure so that
    /// <see cref="HybridCache"/> does not cache the negative result.
    /// </summary>
    private async Task<Client> ResolveClientAsync(string clientId, CancellationToken ct)
    {
        // Validate the client_id is a well-formed CIMD URI
        if (!CimdDocumentValidator.TryParseClientUri(clientId, out var clientUri))
        {
            Log.InvalidClientUri(logger, clientId);
            throw new CimdResolutionException();
        }

        // SSRF protection (CIMD spec section 6.5)
        if (!await ssrfGuard.IsSafeAsync(clientUri, ct))
        {
            Log.SsrfCheckFailed(logger, clientId);
            throw new CimdResolutionException();
        }

        // Domain allowlist policy
        var domainResult = await policy.CheckDomainAsync(clientUri, ct);
        if (!domainResult.IsAllowed)
        {
            Log.DomainDeniedByPolicy(logger, clientId, domainResult.Reason);
            throw new CimdResolutionException();
        }

        // Fetch and deserialize the CIMD document
        var document = await fetcher.FetchDocumentAsync(clientUri, ct);
        if (document == null)
        {
            throw new CimdResolutionException();
        }

        // Validate document contents
        if (!CimdDocumentValidator.ClientIdMatchesDocument(clientId, document))
        {
            Log.ClientIdMismatch(logger, clientId);
            throw new CimdResolutionException();
        }

        if (!CimdDocumentValidator.PassesAuthMethodChecks(document, out var authMethodFailureReason))
        {
            Log.AuthMethodCheckFailed(logger, clientId, authMethodFailureReason);
            throw new CimdResolutionException();
        }

        // Document-level policy check
        var documentResult = await policy.ValidateDocumentAsync(clientUri, document, ct);
        if (!documentResult.IsAllowed)
        {
            Log.DocumentDeniedByPolicy(logger, clientId, documentResult.Reason);
            throw new CimdResolutionException();
        }

        // Resolve keys and build the IdentityServer client
        var keySet = await fetcher.ResolveJwksAsync(document, ct);
        var client = CimdClientBuilder.Build(clientId, document, keySet);

        Log.RegisteredCimdClient(logger, clientId);
        return client;
    }

    /// <summary>
    /// Thrown when CIMD resolution fails, signaling that the result should
    /// not be cached by <see cref="HybridCache"/>.
    /// </summary>
    private sealed class CimdResolutionException : Exception;
}
