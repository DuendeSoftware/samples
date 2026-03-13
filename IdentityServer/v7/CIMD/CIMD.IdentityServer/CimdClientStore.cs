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
        var context = await fetcher.FetchAsync(clientUri, ct);
        if (context == null)
        {
            throw new CimdResolutionException();
        }

        // Validate document contents
        if (!CimdDocumentValidator.ClientIdMatchesDocument(clientId, context.Document))
        {
            Log.ClientIdMismatch(logger, clientId);
            throw new CimdResolutionException();
        }

        if (!CimdDocumentValidator.PassesAuthMethodChecks(context.Document, out var authMethodFailureReason))
        {
            Log.AuthMethodCheckFailed(logger, clientId, authMethodFailureReason);
            throw new CimdResolutionException();
        }

        // Document-level policy check (has access to response headers via context)
        var documentResult = await policy.ValidateDocumentAsync(context, ct);
        if (!documentResult.IsAllowed)
        {
            Log.DocumentDeniedByPolicy(logger, clientId, documentResult.Reason);
            throw new CimdResolutionException();
        }

        // Resolve keys and build the IdentityServer client
        var keySet = await fetcher.ResolveJwksAsync(context, ct);
        var client = CimdClientBuilder.Build(clientId, context.Document, keySet);

        Log.RegisteredCimdClient(logger, clientId);
        return client;
    }

    /// <summary>
    /// Thrown when CIMD resolution fails, signaling that the result should
    /// not be cached by <see cref="HybridCache"/>.
    /// </summary>
    private sealed class CimdResolutionException : Exception;

    private static partial class Log
    {
        [LoggerMessage(LogLevel.Debug, "Successfully registered CIMD client '{ClientId}'")]
        public static partial void RegisteredCimdClient(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "'{ClientId}' is not a valid CIMD client URI: must be https, have a non-empty path, and contain no fragment, credentials, or dot-segments")]
        public static partial void InvalidClientUri(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "CIMD client URI '{ClientId}' resolves to a special-use IP address (RFC 6890); rejecting to prevent SSRF")]
        public static partial void SsrfCheckFailed(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "CIMD client URI '{ClientId}' was denied by policy: {Reason}")]
        public static partial void DomainDeniedByPolicy(ILogger logger, string clientId, string? reason);

        [LoggerMessage(LogLevel.Error, "CIMD document for '{ClientId}' was denied by policy: {Reason}")]
        public static partial void DocumentDeniedByPolicy(ILogger logger, string clientId, string? reason);

        [LoggerMessage(LogLevel.Error, "CIMD document client_id does not match the request URL '{ClientId}'")]
        public static partial void ClientIdMismatch(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "CIMD document for '{ClientId}' failed auth method validation: {Reason}")]
        public static partial void AuthMethodCheckFailed(ILogger logger, string clientId, string reason);
    }
}
