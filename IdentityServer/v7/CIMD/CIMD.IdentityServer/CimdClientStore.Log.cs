using System.Net;

namespace CIMD.IdentityServer;

public partial class CimdClientStore
{
    private static partial class Log
    {
        [LoggerMessage(LogLevel.Error, "CIMD client URI '{ClientId}' was denied by policy: {Reason}")]
        public static partial void DomainDeniedByPolicy(ILogger logger, string clientId, string? reason);

        [LoggerMessage(LogLevel.Error, "CIMD document for '{ClientId}' was denied by policy: {Reason}")]
        public static partial void DocumentDeniedByPolicy(ILogger logger, string clientId, string? reason);

        [LoggerMessage(LogLevel.Debug, "CIMD client '{ClientId}' found in cache")]
        public static partial void FoundCachedClient(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "Successfully registered CIMD client '{ClientId}'")]
        public static partial void RegisteredCimdClient(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "'{ClientId}' is not a valid CIMD client URI: must be https, have a non-empty path, and contain no fragment, credentials, or dot-segments")]
        public static partial void InvalidClientUri(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "CIMD client URI '{ClientId}' resolves to a special-use IP address (RFC 6890); rejecting to prevent SSRF")]
        public static partial void SsrfCheckFailed(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "HTTP request to CIMD document URL '{ClientUri}' failed")]
        public static partial void HttpRequestFailed(ILogger logger, Uri clientUri, Exception ex);

        [LoggerMessage(LogLevel.Error, "CIMD document URL '{ClientUri}' returned non-200 status {StatusCode}")]
        public static partial void NonSuccessStatusCode(ILogger logger, Uri clientUri, HttpStatusCode statusCode);

        [LoggerMessage(LogLevel.Error, "CIMD document at '{ClientUri}' is {BytesRead} bytes, exceeding the {MaxBytes}-byte limit")]
        public static partial void DocumentTooLarge(ILogger logger, Uri clientUri, long bytesRead, int maxBytes);

        [LoggerMessage(LogLevel.Error, "Failed to deserialize CIMD document from '{ClientUri}'")]
        public static partial void DocumentDeserializationFailed(ILogger logger, Uri clientUri, Exception ex);

        [LoggerMessage(LogLevel.Error, "CIMD document client_id does not match the request URL '{ClientId}'")]
        public static partial void ClientIdMismatch(ILogger logger, string clientId);

        [LoggerMessage(LogLevel.Error, "CIMD document for '{ClientId}' failed auth method validation: {Reason}")]
        public static partial void AuthMethodCheckFailed(ILogger logger, string clientId, string reason);

        [LoggerMessage(LogLevel.Debug, "Using inline JWKS from CIMD document")]
        public static partial void UsingInlineJwks(ILogger logger);

        [LoggerMessage(LogLevel.Debug, "Fetching JWKS from jwks_uri '{JwksUri}'")]
        public static partial void FetchingJwksUri(ILogger logger, Uri jwksUri);

        [LoggerMessage(LogLevel.Error, "Failed to fetch JWKS from '{JwksUri}'")]
        public static partial void JwksUriFetchFailed(ILogger logger, Uri jwksUri, Exception ex);

        [LoggerMessage(LogLevel.Error, "JWKS response from '{JwksUri}' contained an error: {Error}")]
        public static partial void JwksUriResponseError(ILogger logger, Uri jwksUri, string? error);

        [LoggerMessage(LogLevel.Debug, "CIMD document contains no JWKS; client will be treated as public")]
        public static partial void NoJwks(ILogger logger);
    }
}
