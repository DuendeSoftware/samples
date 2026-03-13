namespace CIMD.IdentityServer;

public partial class CimdClientStore
{
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
