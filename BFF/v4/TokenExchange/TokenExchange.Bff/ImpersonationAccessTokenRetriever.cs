// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;

namespace TokenExchange.Bff;

public class ImpersonationAccessTokenRetriever : IAccessTokenRetriever
{
    public async Task<AccessTokenResult> GetAccessTokenAsync(AccessTokenRetrievalContext context,
        CancellationToken ct = new CancellationToken())
    {
        var result = await context.HttpContext.GetUserAccessTokenAsync(new UserTokenRequestParameters(), ct);

        if (!result.Succeeded)
            throw new Exception("Failed to get access token");

        var client = new HttpClient();
        var exchangeResponse = await client.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
        {
            Address = "https://localhost:5001/connect/token",
            GrantType = OidcConstants.GrantTypes.TokenExchange,

            ClientId = "spa",
            ClientSecret = "secret",

            SubjectToken = result.Token.AccessToken,
            SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken
        }, ct);
        if (exchangeResponse.IsError)
        {
            return new AccessTokenRetrievalError{Error = $"Token exchanged failed: {exchangeResponse.ErrorDescription}"};
        }

        if (exchangeResponse.AccessToken is null)
        {
            return new AccessTokenRetrievalError{Error = "Token exchanged failed. Access token is null"};
        }
        else
        {
            return new BearerTokenResult{AccessToken = AccessToken.Parse(exchangeResponse.AccessToken)};
        }
    }
}
