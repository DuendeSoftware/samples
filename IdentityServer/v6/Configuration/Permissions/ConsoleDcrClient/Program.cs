// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using ConsoleDcrClient;
using IdentityModel.Client;


Console.Title = "Dynamic Client Registration - Client Credentials Flow";

"Obtaining initial access token (which does not allow setting client ids)".ConsoleYellow();
var badTokenResponse = await RequestTokenAsync(scope: "IdentityServer.Configuration");
badTokenResponse.Show();
Console.ReadLine();

"\n\nAttempting to register a dynamic client with a specific client secret, but without needed scope".ConsoleYellow();
var badDcrResponse = await RegisterClient(badTokenResponse.AccessToken);
"This succeeded, but ignored our attempt to set a client secret.".ConsoleYellow();
Console.ReadLine();

$"\n\nObtaining access token for dynamic client using clientId: {badDcrResponse.ClientId} and secret {badDcrResponse.ClientSecret}".ConsoleYellow();
var badDynamicClientToken = await RequestTokenAsync(badDcrResponse.ClientId, badDcrResponse.ClientSecret);
badDynamicClientToken.Show();
Console.ReadLine();

"Obtaining a new access token (which does allow setting client ids)".ConsoleYellow();
var goodTokenResponse = await RequestTokenAsync(scope: "IdentityServer.Configuration IdentityServer.Configuration:SetClientSecret");
goodTokenResponse.Show();
Console.ReadLine();

"\n\nReattempting to register a dynamic client with a specific client secret".ConsoleYellow();
var goodDcrResponse = await RegisterClient(goodTokenResponse.AccessToken);
"This succeeded, and respected our attempt to set a client secret.".ConsoleYellow();
Console.ReadLine();

$"\n\nObtaining access token for dynamic client using clientId: {goodDcrResponse.ClientId} and secret {goodDcrResponse.ClientSecret}".ConsoleYellow();
var dynamicClientToken = await RequestTokenAsync(goodDcrResponse.ClientId, goodDcrResponse.ClientSecret);
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

    request.Document.Extensions.Add("client_secret", "hunter2");

    var response = await client.RegisterClientAsync(request);

    if (response.IsError)
    {
        Console.WriteLine(response.Error);
        return null;
    }

    response.Show();

    return response;
}

static async Task<TokenResponse> RequestTokenAsync(string clientId = "client", string clientSecret = "secret", string scope = null)
{
    var client = new HttpClient();

    var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
    if (disco.IsError) throw new Exception(disco.Error);

    var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    {
        Address = disco.TokenEndpoint,

        ClientId = clientId,
        ClientSecret = clientSecret,
        Scope = scope
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
