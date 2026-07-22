// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MultiSpace.Seed;
using System.Security.Claims;
using Duende.IdentityModel.Client;
using Duende.MultiSpace;
using Duende.Storage.Querying;

namespace MultiSpace.Pages;

/// <summary>
/// Index page model - home page for every space.
///
/// Responsibilities:
///   • Display current space id, name, and routing explanation.
///   • Render cross-space navigation links.
///   • Show demo credentials for the current space.
///   • Login/logout area (current user's claims when signed in).
///   • Server-side token request form (client id/secret never sent to browser).
///   • Token output: raw token, decoded JWT header/payload, claims table.
/// </summary>
public sealed class IndexModel(
    IHttpContextAccessor httpContextAccessor,
    ISpaceContextAccessor spaceContextAccessor,
    ISpaceAdmin spaceAdmin,
    IHttpClientFactory httpClientFactory) : PageModel
{
    public string? SpaceId { get; private set; }
    public string? SpaceName { get; private set; }
    public bool IsSignedIn { get; private set; }
    public string? SignedInUser { get; private set; }

    /// <summary>Claims of the currently signed-in user (empty when not signed in).</summary>
    public IReadOnlyList<(string Type, string Value)> UserClaims { get; private set; } = [];

    /// <summary>All space links for cross-space navigation.</summary>
    public IReadOnlyList<SpaceLink> SpaceLinks { get; private set; } = [];

    /// <summary>Demo credentials for the current space, or null if no demo user is seeded.</summary>
    public string? DemoUsername { get; private set; }

    public string? AccessToken { get; set; }

    /// <summary>Client id used for the last token request (safe to display).</summary>
    public string? TokenClientId { get; private set; }

    /// <summary>Error message from a failed token request.</summary>
    public string? TokenError { get; private set; }

    /// <summary>Raw JSON response body from the token endpoint.</summary>
    public string? TokenRawResponse { get; private set; }

    /// <summary>Decoded token display (raw token, JWT header/payload, claims table).</summary>
    public TokenDisplayResult? TokenDisplay { get; private set; }

    /// <summary>Which client to use for the token request (bound from form).</summary>
    [BindProperty]
    public string SelectedClient { get; set; } = "space";

    /// <summary>The space-specific client id.</summary>
    public string SpaceClientId => $"{SpaceName?.ToLowerInvariant()}-client";

    /// <summary>The shared example client id (same across all spaces).</summary>
    public const string ExampleClientId = "example-client";

    public async Task OnGetAsync(CancellationToken ct)
    {
        await PopulateViewStateAsync(ct);
    }

    public async Task<IActionResult> OnPostRequestTokenAsync(CancellationToken ct)
    {
        await PopulateViewStateAsync(ct);

        TokenClientId = SelectedClient == "example" ? ExampleClientId : SpaceClientId;
        var client = httpClientFactory.CreateClient("TokenRequest");
        var spaceUrl = await GetUriFor(spaceContextAccessor.GetSpaceId(), ct);
        var authorityUrl = new Uri(spaceUrl, "connect/token");
        var result = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest()
        {
            ClientId = TokenClientId,
            ClientSecret = "secret",
            Address = authorityUrl.ToString(),
            GrantType = "client_credentials",
            Scope = "scope1"
        }, ct);

        TokenRawResponse = result.Raw;

        if (result.IsError)
        {
            TokenError = result.Error ?? "Token request failed.";
        }
        else
        {
            AccessToken = result.AccessToken;
            TokenDisplay = BuildTokenDisplay(result.AccessToken);
        }

        return Page();
    }


    private async Task PopulateViewStateAsync(CancellationToken ct)
    {
        // Space info
        SpaceId = spaceContextAccessor.GetSpaceId().ToString();
        var getSpace = await spaceAdmin.GetAsync(spaceContextAccessor.GetSpaceId(), ct);
        var allSpaces = await spaceAdmin.QueryAsync(QueryRequest.Create<SpaceFilter, SpaceSortField>(), ct);
        var spaceLinks = new List<SpaceLink>()
        {
            new("Default", "https://localhost:5000")
        };
        foreach (var spaceItem in allSpaces)
        {
            var uri = await GetUriFor(spaceItem.Id, ct);

            var spaceLink = new SpaceLink(spaceItem.Name, uri.ToString());
            spaceLinks.Add(spaceLink);
        }

        SpaceName = getSpace.Item?.Name ?? "Default";

        // Demo credentials for the current space
        DemoUsername = DemoCredentials.GetUsernameForSpace(SpaceName);

        // Signed-in user info
        var user = httpContextAccessor.HttpContext?.User;
        IsSignedIn = user?.Identity?.IsAuthenticated == true;
        if (IsSignedIn)
        {
            SignedInUser = user!.FindFirstValue("name") ?? user!.FindFirstValue("sub") ?? "Unknown";
            UserClaims = user!.Claims
                .Select(c => (c.Type, c.Value))
                .ToList();
        }

        // Cross-space links
        SpaceLinks = spaceLinks;
    }

    private async Task<Uri> GetUriFor(SpaceId spaceId, CancellationToken ct)
    {
        var getSpaceData = await spaceAdmin.GetAsync(spaceId, ct);

        if (!getSpaceData.Found)
        {
            return new Uri("https://localhost:5000");
        }

        var url = getSpaceData.Item.MatchPatterns.First() switch
        {
            { Origin: { } o, Path: { } p } => new Uri(new Uri(o), p),
            { Origin: { } o } => new Uri(o),
            { Path: { } p } => new Uri(new Uri("https://ignored.dev.localhost:5000"), $"/t{p}/"),
            _ => new Uri("https://localhost:5000")
        };

        return url;
    }

    /// <summary>
    /// Attempts to decode the access token as a JWT. If parsing fails (opaque/reference
    /// token), returns a result with the raw token and IsOpaqueToken = true.
    /// </summary>
    private static TokenDisplayResult BuildTokenDisplay(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new TokenDisplayResult(
                RawToken: string.Empty,
                IsOpaqueToken: true,
                HeaderJson: null,
                PayloadJson: null,
                Claims: []);
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(accessToken))
            {
                var jwt = handler.ReadJwtToken(accessToken);

                var headerJson = PrettyPrint(jwt.Header.SerializeToJson());
                var payloadJson = PrettyPrint(jwt.Payload.SerializeToJson());

                var claims = jwt.Claims
                    .Select(c => new TokenClaim(c.Type, c.Value))
                    .ToList();

                return new TokenDisplayResult(
                    RawToken: accessToken,
                    IsOpaqueToken: false,
                    HeaderJson: headerJson,
                    PayloadJson: payloadJson,
                    Claims: claims);
            }
        }
        catch
        {
            // Fall through to opaque token display
        }

        return new TokenDisplayResult(
            RawToken: accessToken,
            IsOpaqueToken: true,
            HeaderJson: null,
            PayloadJson: null,
            Claims: []);
    }

    private static string PrettyPrint(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}

public record SpaceLink(string Name, string Url);

/// <summary>
/// Holds the decoded token display data for rendering in the view.
/// </summary>
public record TokenDisplayResult(
    string RawToken,
    bool IsOpaqueToken,
    string? HeaderJson,
    string? PayloadJson,
    IReadOnlyList<TokenClaim> Claims);

/// <summary>
/// A single claim from a decoded JWT.
/// </summary>
public record TokenClaim(string Type, string Value);
