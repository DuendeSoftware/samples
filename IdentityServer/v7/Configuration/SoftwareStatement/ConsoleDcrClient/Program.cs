// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text.Json;
using ConsoleDcrClient;
using Duende.IdentityModel.Client;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

Console.Title = "DCR Client";

"Obtaining initial access token".ConsoleYellow();
var tokenResponse = await RequestTokenAsync();
tokenResponse.Show();
Console.ReadLine();

"\n\nRegistering dynamic client".ConsoleYellow();
var dcrResponse = await RegisterClient(tokenResponse.AccessToken);
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
            Scope = "SimpleApi",
            SoftwareStatement = CreateSoftwareStatement()
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

static string CreateSoftwareStatement()
{
    var key = new RsaSecurityKey(new RSAParameters
    {
        D = Convert.FromBase64String("Y7jZreBkc0Ex8+abwR7D8t/qumA4YZjQP9xGNKrnNeJfnm5qz+cOOBJGczvQiqCOuddEctP8lP1WBKZ/VlFWFs+4mfYVBe+dlDerovS6/h8Gii5hZGQWbXd8anSwTfwM3M1LnnfeYmqbJtn93JcUOUM+pOeo/fAn8/DkgSaKeEzTwlJSvgwdVnoT0tmvd4TelXGEC2Z3b1nbkv5oAdfu8RrZHsG0G50Glh/QHjHtLrZCWrQI5D/UOTTD7rUdwTd+mBeh3SBlCSv+iLUEq8mPMYDKJr5PMclklvCtlkGwtUDakv6i3HXYnQAq1CP8erxKmgaAVVkKnc3yqujH9qeYKQ=="),
        DP = Convert.FromBase64String("SPE+HMqsZqAStI1NL0Dk6nwmCpNZqcU6Qo9sfMsHErYhnJAYfS29OTAs2Osn/IXrHrh4UleLCtwsIBD5febUpL5Ttvc1Ix++WX83+WmuBKukpWjromQMsrh0RKAmnSNj8qoFiqoBSb9CPTdXf6A+HsGYcwPVR4d1jW5Pl7t1ffk="),
        DQ = Convert.FromBase64String("mtVMEjzm650Ql/YiKonhZsWGXWWE2EDB+gmyarUZ7EFa2lGdvGgk5fXuAqgrJZHIRCvF67INoKBcOoVPkAyJ/H7rWmLb6oWx3Nu0LT5NRTMJYs3qvzTeIX5i/vqh3A0Urhe6lweZVXn1AssVN4Ao1yZdKawA3kRbl9FMRe/svqk="),
        Exponent = Convert.FromBase64String("AQAB"),
        InverseQ = Convert.FromBase64String("D96gaMqXrijyOuEmoTRqBjGK8aVqb9kEilvbUP85a6Z71zji7q/A+jIi316587wT5rtbarUjZD+oIkJzfRx4BvdoaEWGj+ekXPlrXCVCPTlSJRV0qmQ6T9mbn9WzPsehiaoJYDRis6LBj3Aixg3kwJm2QlVQyEzhhZbmov0QIlc="),
        Modulus = Convert.FromBase64String("rTLn01iUFgUAX7Cl6nFjE3FnegQP6jCPq2qffhLw50ZrAgkZdPz2ITE7DCjJL4Ln9YLpldCIZhqImHz3ojfMD2Yuf2ac6H1l96ZyIVqxTrm7fIagGhbJjzrxBRGQIYRawMVmWMo0vksuWM0U5lImdLbL4j74soRjg2QgTqomAvWqHcOSOBnIf5RfcUXHZCbsjZ8DAMUijR+Bjb8PqTq98UFiqLEDWUmz6qLOiO0aOV1VBBls6TuKlS+xJ/HNHbABbVIUewzdWsRKKiAUmQB5rU9InGZ8+B+OBl+dYDgaTruOe4R5dBfGRfeIkLjSQ2o55TfqVp/mSXDM0aSXBrrtzQ=="),
        P = Convert.FromBase64String("z3KXgeW06pY1/2DCD7WSAx9VcApbPbXV3yKxyzQqz9gOEmpwCgXkb/Of8J/7dsGP+2sgVjSsQ3I8TaSnx9Xm+jY3yE8N2IFqh1PHkoa50OpfunlS6UoQ306vq56Ks4vxffSgYTj3pKTnkKi79LXPYAZNGsEJUxraD5iEqpUCjH8="),
        Q = Convert.FromBase64String("1bxEoR/G7od5hiwvMjZf5ft0gkDZDmql7BcaReFaqTQKanzXKNyGwGisVnslXdRKOo86YkRsYEWxgVAutCooCYnNXQ9IeoT5Q0KLmSxhbG2P1KSZjeYlb0Eu+/QvUJjMGdz52KCPX6HYFIiz++NFr18evFhQ+0LwIFYf6u2oz7M="),
    });

    var credential = new SigningCredentials(key, "RS256");

    var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };

    var claims = new Dictionary<string, string>
    {
        { Duende.IdentityModel.OidcConstants.ClientMetadata.ClientName, "demo-client" },
        { Duende.IdentityModel.OidcConstants.ClientMetadata.SoftwareId, Guid.NewGuid().ToString() }
    };

    var payload = JsonSerializer.Serialize(claims);

    return handler.CreateToken(payload, credential);
}

static async Task<TokenResponse> RequestTokenAsync(string clientId = "client", string clientSecret = "secret")
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
