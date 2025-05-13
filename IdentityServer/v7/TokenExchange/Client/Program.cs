// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityModel.Client;

namespace ResourcesScopesConsoleClient;

class Program
{
    private static DiscoveryCache Cache;

    static async Task Main(string[] args)
    {
        Console.Title = "Client";
        Cache = new DiscoveryCache("https://localhost:5001");

        // initial token
        var response = await RequestTokenAsync();
        var initialToken = response.AccessToken;

        "\n\nInitial token:".ConsoleYellow();
        response.Show();
        Console.ReadLine();

        response = await DelegateToken(initialToken, "impersonation");

        "\n\nImpersonation style:".ConsoleYellow();
        response.Show();
        Console.ReadLine();

        response = await DelegateToken(initialToken, "delegation");

        "\n\nDelegation style:".ConsoleYellow();
        response.Show();
        Console.ReadLine();

        response = await DelegateToken(initialToken, "custom");

        "\n\nCustom style:".ConsoleYellow();
        response.Show();
    }

    static async Task<TokenResponse> RequestTokenAsync()
    {
        var client = new HttpClient();

        var disco = await Cache.GetAsync();
        if (disco.IsError) throw new Exception(disco.Error);

        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "front.end",
            ClientSecret = "secret",

            Scope = "scope1",
        });

        if (response.IsError) throw new Exception(response.Error);
        return response;
    }

    static async Task<TokenResponse> DelegateToken(string token, string style)
    {
        var client = new HttpClient();

        var disco = await Cache.GetAsync();
        if (disco.IsError) throw new Exception(disco.Error);

        var response = await client.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "api1",
            ClientSecret = "secret",

            SubjectToken = token,
            SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken,
            Scope = "scope2",

            Parameters =
            {
                { "exchange_style", style }
            }
        });

        if (response.IsError) throw new Exception(response.Error);
        return response;
    }
}
