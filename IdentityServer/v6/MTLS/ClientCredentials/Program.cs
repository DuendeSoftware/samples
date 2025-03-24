// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel.Client;
using Shared;

namespace Client;

public static class Urls
{
    public const string IdentityServer = "https://localhost:5001";

    public const string ApiBaseMtls = "https://api.localhost:6002";
    public const string ApiMtls = ApiBaseMtls + "/identity";
}

public class Program
{
    public static async Task Main()
    {
        Console.Title = "Console MTLS Client";

        var response = await RequestTokenAsync();
        response.Show();

        await CallServiceAsync(response.AccessToken);
    }

    static async Task<TokenResponse> RequestTokenAsync()
    {
        var client = new HttpClient(GetHandler());

        var disco = await client.GetDiscoveryDocumentAsync(Urls.IdentityServer);
        if (disco.IsError) throw new Exception(disco.Error);

        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.MtlsEndpointAliases.TokenEndpoint,

            ClientId = "mtls",
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Scope = "scope1"
        });

        if (response.IsError) throw new Exception(response.Error);
        return response;
    }

    static async Task CallServiceAsync(string token)
    {
        var client = new HttpClient(GetHandler());
        client.SetBearerToken(token);

        var response = await client.GetStringAsync(Urls.ApiMtls);

        "\n\nService claims:".ConsoleGreen();
        Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(response), new JsonSerializerOptions { WriteIndented = true }));
    }

    static SocketsHttpHandler GetHandler()
    {
        var handler = new SocketsHttpHandler();

        var cert = new X509Certificate2("client.p12", "changeit");
        handler.SslOptions.ClientCertificates = new X509CertificateCollection { cert };

        return handler;
    }
}
