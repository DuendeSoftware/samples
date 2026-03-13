using System.Buffers;
using System.Net;
using System.Text.Json;
using Duende.IdentityModel.Client;
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
    /// Fetches and deserializes a CIMD document from the given URI.
    /// Returns null if the fetch fails, the response is non-200, the document
    /// exceeds the size limit, or deserialization fails.
    /// </summary>
    public async Task<DynamicClientRegistrationDocument?> FetchDocumentAsync(
        Uri clientUri, CancellationToken ct)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

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

    /// <summary>
    /// Resolves the JWKS for a CIMD document: prefers inline jwks,
    /// falls back to fetching jwks_uri.
    /// </summary>
    public async Task<JsonWebKeySet?> ResolveJwksAsync(
        DynamicClientRegistrationDocument document, CancellationToken ct)
    {
        if (document.Jwks is not null)
        {
            Log.UsingInlineJwks(logger);
            return document.Jwks;
        }

        if (document.JwksUri is not null)
        {
            Log.FetchingJwksUri(logger, document.JwksUri);

            using var httpClient = httpClientFactory.CreateClient(HttpClientName);
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

    private static partial class Log
    {
        [LoggerMessage(LogLevel.Error, "HTTP request to CIMD document URL '{ClientUri}' failed")]
        public static partial void HttpRequestFailed(ILogger logger, Uri clientUri, Exception ex);

        [LoggerMessage(LogLevel.Error, "CIMD document URL '{ClientUri}' returned non-200 status {StatusCode}")]
        public static partial void NonSuccessStatusCode(ILogger logger, Uri clientUri, HttpStatusCode statusCode);

        [LoggerMessage(LogLevel.Error, "CIMD document at '{ClientUri}' is {BytesRead} bytes, exceeding the {MaxBytes}-byte limit")]
        public static partial void DocumentTooLarge(ILogger logger, Uri clientUri, long bytesRead, int maxBytes);

        [LoggerMessage(LogLevel.Error, "Failed to deserialize CIMD document from '{ClientUri}'")]
        public static partial void DocumentDeserializationFailed(ILogger logger, Uri clientUri, Exception ex);

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
