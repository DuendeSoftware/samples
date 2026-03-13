using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Duende.IdentityModel.Jwk;

namespace CIMD.IdentityServer;

/// <summary>
/// Fetches and deserializes CIMD documents and JWKS from remote servers,
/// enforcing size limits and status code requirements per the CIMD spec.
/// </summary>
public partial class CimdDocumentFetcher(
    IHttpClientFactory httpClientFactory,
    ILogger<CimdDocumentFetcher> logger)
{
    public const string HttpClientName = "cimd";

    // Per spec section 6.6: SHOULD limit response size to 5 KB.
    private const int MaxDocumentSizeBytes = 5 * 1024;

    /// <summary>
    /// Fetches and deserializes a CIMD document from the given URI, returning
    /// a <see cref="CimdRequestContext"/> that bundles the parsed document with
    /// the HTTP response metadata. Returns null if the fetch fails, the
    /// response is non-200, the document exceeds the size limit, or
    /// deserialization fails.
    /// </summary>
    public async Task<CimdRequestContext?> FetchAsync(
        Uri clientUri, CancellationToken ct)
    {
        var fetchResult = await FetchWithSizeLimitAsync(clientUri, ct);
        if (fetchResult is null)
        {
            return null;
        }

        CimdDocument document;
        try
        {
            document = JsonSerializer.Deserialize<CimdDocument>(fetchResult.Value.Body)!;
        }
        catch (Exception ex)
        {
            Log.DeserializationFailed(logger, clientUri, "CIMD document", ex);
            return null;
        }

        return new CimdRequestContext
        {
            ClientUri = clientUri,
            Document = document,
            ResponseHeaders = fetchResult.Value.ResponseHeaders,
            ContentHeaders = fetchResult.Value.ContentHeaders,
        };
    }

    /// <summary>
    /// Resolves the JWKS for a CIMD document: prefers inline jwks,
    /// falls back to fetching jwks_uri.
    /// </summary>
    public async Task<JsonWebKeySet?> ResolveJwksAsync(
        CimdRequestContext context, CancellationToken ct)
    {
        if (context.Document.Jwks is not null)
        {
            Log.UsingInlineJwks(logger);
            return context.Document.Jwks;
        }

        if (context.Document.JwksUri is not null)
        {
            Log.FetchingJwksUri(logger, context.Document.JwksUri);

            var fetchResult = await FetchWithSizeLimitAsync(context.Document.JwksUri, ct);
            if (fetchResult is null)
            {
                return null;
            }

            JsonWebKeySet keySet;
            try
            {
                keySet = JsonSerializer.Deserialize<JsonWebKeySet>(fetchResult.Value.Body)!;
            }
            catch (Exception ex)
            {
                Log.DeserializationFailed(logger, context.Document.JwksUri, "JWKS", ex);
                return null;
            }

            return keySet;
        }

        Log.NoJwks(logger);
        return null;
    }

    /// <summary>
    /// Fetches a document from the given URI, enforcing the 5 KB size limit
    /// per spec section 6.6. Returns null on failure; logs the reason.
    /// Uses <see cref="ArrayPool{T}"/> to avoid per-request allocations.
    /// </summary>
    private async Task<SizeLimitedResponse?> FetchWithSizeLimitAsync(
        Uri uri, CancellationToken ct)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex)
        {
            Log.HttpRequestFailed(logger, uri, ex);
            return null;
        }

        // Per spec section 4: MUST treat all non-200 status codes as errors
        if (!response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.OK)
        {
            Log.NonSuccessStatusCode(logger, uri, response.StatusCode);
            return null;
        }

        // Check Content-Length first to reject obviously oversized responses
        // before reading any body bytes.
        if (response.Content.Headers.ContentLength is > MaxDocumentSizeBytes)
        {
            Log.ResponseTooLarge(logger, uri, response.Content.Headers.ContentLength.Value, MaxDocumentSizeBytes);
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
                Log.ResponseTooLarge(logger, uri, bytesRead, MaxDocumentSizeBytes);
                return null;
            }

            // Copy out of the rented buffer so we can return it promptly
            var body = buffer.AsSpan(0, bytesRead).ToArray();

            return new SizeLimitedResponse(body, response.Headers, response.Content.Headers);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private readonly record struct SizeLimitedResponse(
        byte[] Body,
        HttpResponseHeaders ResponseHeaders,
        HttpContentHeaders ContentHeaders);

    private static partial class Log
    {
        [LoggerMessage(LogLevel.Error, "HTTP request to '{Uri}' failed")]
        public static partial void HttpRequestFailed(ILogger logger, Uri uri, Exception ex);

        [LoggerMessage(LogLevel.Error, "'{Uri}' returned non-200 status {StatusCode}")]
        public static partial void NonSuccessStatusCode(ILogger logger, Uri uri, HttpStatusCode statusCode);

        [LoggerMessage(LogLevel.Error, "Response from '{Uri}' is {BytesRead} bytes, exceeding the {MaxBytes}-byte limit")]
        public static partial void ResponseTooLarge(ILogger logger, Uri uri, long bytesRead, int maxBytes);

        [LoggerMessage(LogLevel.Error, "Failed to deserialize {DocumentType} from '{Uri}'")]
        public static partial void DeserializationFailed(ILogger logger, Uri uri, string documentType, Exception ex);

        [LoggerMessage(LogLevel.Debug, "Using inline JWKS from CIMD document")]
        public static partial void UsingInlineJwks(ILogger logger);

        [LoggerMessage(LogLevel.Debug, "Fetching JWKS from jwks_uri '{JwksUri}'")]
        public static partial void FetchingJwksUri(ILogger logger, Uri jwksUri);

        [LoggerMessage(LogLevel.Debug, "CIMD document contains no JWKS; client will be treated as public")]
        public static partial void NoJwks(ILogger logger);
    }
}
