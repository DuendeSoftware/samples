// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Client;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new HttpClient();

        var disco = await client.GetDiscoveryDocumentAsync("https://demo.duendesoftware.com");
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "m2m",
            ClientSecret = "secret",

            Scope = "api"
        });

        if (response.IsError) throw new Exception(response.Error);

        var functionClient = new HttpClient();
        functionClient.SetBearerToken(response.AccessToken);

        var functionResponse = await functionClient.GetStringAsync("http://localhost:7071/api/HttpExample");

        Console.WriteLine(functionResponse);
    }
}
