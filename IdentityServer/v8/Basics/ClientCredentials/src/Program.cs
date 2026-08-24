// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Client;

using Duende.IdentityModel.Client;

Console.Title = "Console Client Credentials Flow";

var response = await RequestTokenAsync();
response.Show();

WaitToBegin();

await CallServiceAsync(response.AccessToken);

static async Task<TokenResponse> RequestTokenAsync()
{
    var client = new HttpClient();

    var disco = await client.GetDiscoveryDocumentAsync(Urls.IdentityServer);
    if (disco.IsError)
    {
        throw new Exception(disco.Error);
    }

    var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    {
        Address = disco.TokenEndpoint,

        ClientId = "client.credentials.sample",
        ClientSecret = "secret",

        Scope = "scope1"
    });

    if (response.IsError)
    {
        throw new Exception(response.Error);
    }

    return response;
}

static async Task CallServiceAsync(string token)
{
    var client = new HttpClient
    {
        BaseAddress = new Uri(Urls.SampleApi)
    };

    client.SetBearerToken(token);
    var response = await client.GetStringAsync("identity");

    "\n\nService claims:".ConsoleGreen();
    Console.WriteLine(response.PrettyPrintJson());
}

static void WaitToBegin()
{
    //When not run with Aspire, wait for user to initiate this client so services are started
    if (!string.Equals(Environment.GetEnvironmentVariable("IS_IN_ASPIRE"), true.ToString()))
    {
        Console.ReadLine();
    }
}
