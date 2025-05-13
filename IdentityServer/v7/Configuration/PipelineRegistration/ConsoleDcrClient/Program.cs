// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using ConsoleDcrClient;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


Console.Title = "DCR Client using PAT";

"Obtaining initial access token".ConsoleYellow();
using var host = Host.CreateDefaultBuilder(args).Build();
var config = host.Services.GetRequiredService<IConfiguration>();
var pat = config.GetValue<string>("IdentityServer.Configuration:PAT");
while (string.IsNullOrEmpty(pat))
{
    "No Personal Access Token (PAT) configured. You can create a PAT by going to https://localhost:5001/PAT. Then enter your PAT here, or add it to configuration using user-secrets, environment variables, etc".ConsoleYellow();
    pat = Console.ReadLine();
}

"\n\nRegistering dynamic client".ConsoleYellow();
var dcrResponse = await RegisterClient(pat);
if (dcrResponse.IsError)
{
    "Failed to register a client".ConsoleRed();
    dcrResponse.Error.ConsoleRed();
    return;
}
else
{
    "Successfully registered a client with DCR!".ConsoleGreen();
    "In a real pipeline, you would now parse the response json".ConsoleGreen();
    "and configure the application with its new client id and secret.".ConsoleGreen();
    "In this demo, we'll just obtain a token for the client".ConsoleGreen();
    "and use it to call a sample API".ConsoleGreen();
}

Console.ReadLine();

"\n\nObtaining access token for dynamic client".ConsoleYellow();
var dynamicClientToken = await RequestTokenAsync(dcrResponse.ClientId, dcrResponse.ClientSecret);
dynamicClientToken.Show();
Console.ReadLine();

"\n\nCalling API".ConsoleYellow();
await CallServiceAsync(dynamicClientToken.AccessToken);
Console.ReadLine();

static async Task<DynamicClientRegistrationResponse> RegisterClient(string accessToken)
{
    var client = new HttpClient();
    client.SetBearerToken(accessToken);

    var request = new DynamicClientRegistrationRequest
    {
        Address = "https://localhost:5002/connect/dcr",
        Document = new DynamicClientRegistrationDocument
        {

            GrantTypes = { "client_credentials" },
            Scope = "SimpleApi"
        }
    };

    var response = await client.RegisterClientAsync(request);

    if (response.IsError)
    {
        Console.WriteLine(response.Error);
        return null;
    }
    Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));

    return response;
}

static async Task<TokenResponse> RequestTokenAsync(string clientId, string clientSecret)
{
    var client = new HttpClient();

    var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
    if (disco.IsError) throw new Exception(disco.Error);

    var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    {
        Address = disco.TokenEndpoint,

        ClientId = clientId,
        ClientSecret = clientSecret,
    });

    if (response.IsError) throw new Exception(response.Error);
    return response;
}

static async Task CallServiceAsync(string token)
{
    var baseAddress = Constants.SimpleApi;

    var client = new HttpClient
    {
        BaseAddress = new Uri(baseAddress)
    };

    client.SetBearerToken(token);
    var response = await client.GetStringAsync("identity");

    "\n\nService claims:".ConsoleGreen();
    Console.WriteLine(response.PrettyPrintJson());
}
