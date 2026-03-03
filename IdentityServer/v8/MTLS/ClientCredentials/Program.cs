// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Duende.IdentityModel.Client;
using Shared;

namespace ClientCredentials;

public static class Urls
{
    public const string IdentityServer = "https://localhost:5001";

    public const string ApiBaseMtls = "https://localhost:6001";
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
        if (disco.IsError)
        {
            throw new Exception(disco.Error);
        }

        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.MtlsEndpointAliases.TokenEndpoint,

            ClientId = "mtls",
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
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
        var client = new HttpClient(GetHandler());
        client.SetBearerToken(token);

        var response = await client.GetStringAsync(Urls.ApiMtls);

        "\n\nService claims:".ConsoleGreen();
        Console.WriteLine(JsonSerializer.Serialize(JsonDocument.Parse(response), new JsonSerializerOptions { WriteIndented = true }));
    }

    static SocketsHttpHandler GetHandler()
    {
        var handler = new SocketsHttpHandler();

        // When running from Visual Studio the current directory gets set to the assembly
        // location, but when running from command prompt with dotnet run the current
        // directory is likely the project directory. This works for both.
        var assemblyDir = typeof(Program).Assembly.Location;
        var certPath = Path.GetFullPath(Path.Combine(assemblyDir, "../../../../../localhost-client.p12"));

        var cert = X509CertificateLoader.LoadPkcs12FromFile(certPath, "changeit");
        handler.SslOptions.ClientCertificates = new X509CertificateCollection { cert };

        return handler;
    }
}
