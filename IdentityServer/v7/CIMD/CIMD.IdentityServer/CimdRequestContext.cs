using System.Net.Http.Headers;

namespace CIMD.IdentityServer;

/// <summary>
/// Encapsulates the result of fetching a CIMD document, providing both the
/// parsed document and the HTTP response metadata. Policy implementations
/// can use response headers to make validation decisions (e.g., checking
/// Content-Type, Cache-Control, or custom headers from the CIMD server).
/// </summary>
public sealed class CimdRequestContext
{
    /// <summary>The URI the CIMD document was fetched from (the client_id).</summary>
    public required Uri ClientUri { get; init; }

    /// <summary>The parsed CIMD metadata document.</summary>
    public required CimdDocument Document { get; init; }

    /// <summary>
    /// HTTP response headers from the CIMD document fetch. Includes both
    /// response headers (e.g., Cache-Control, ETag) and content headers
    /// (e.g., Content-Type, Content-Length).
    /// </summary>
    public required HttpResponseHeaders ResponseHeaders { get; init; }

    /// <summary>
    /// HTTP content headers from the CIMD document fetch (e.g., Content-Type).
    /// </summary>
    public required HttpContentHeaders ContentHeaders { get; init; }
}
