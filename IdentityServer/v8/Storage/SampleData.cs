// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Admin;
using Duende.IdentityServer.Admin.ApiScopes;
using Duende.IdentityServer.Admin.Clients;
using Duende.IdentityServer.Admin.IdentityResources;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores.Storage;
using Duende.Storage;
using Duende.Storage.EntityAttributeValue;
using SecretHashAlgorithm = Duende.IdentityServer.Admin.SecretHashAlgorithm;

namespace Storage;

internal static class SampleData
{
    internal const string AllowedRegion = "eu-west";
    internal const string ClientCredentialsClientId = "storage-sample-machine";
    internal const string ClientSecret = "storage-sample-secret";
    internal const string InteractiveClientId = "storage-sample-interactive";
    internal const string RegionHeaderName = "X-Simulated-Region";

    internal static readonly TypedAttributeDefinition<string> AllowedRegionAttribute =
        new(AttributeCode.Create("allowed_region"), new ScalarAttributeType(ScalarDataType.String));

    internal static readonly SchemaConfiguration ClientSchema = new()
    {
        SchemaId = SchemaId.Client,
        DisplayName = "Client",
        Description = "Extended attributes used by the storage sample.",
        AttributeDefinitions = [AllowedRegionAttribute]
    };

    internal static async Task InitializeAsync(
        IServiceProvider services,
        string sampleBaseUrl,
        CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        await EnsureApiScopeAsync(provider.GetRequiredService<IApiScopeAdmin>(), ct);
        await EnsureIdentityResourceAsync(
            provider.GetRequiredService<IIdentityResourceAdmin>(),
            IdentityServerConstants.StandardScopes.OpenId,
            ["sub"],
            ct);
        await EnsureIdentityResourceAsync(
            provider.GetRequiredService<IIdentityResourceAdmin>(),
            IdentityServerConstants.StandardScopes.Profile,
            ["name"],
            ct);
        await EnsureClientAsync(
            provider.GetRequiredService<IClientAdmin>(),
            CreateMachineClient(),
            ct);
        await EnsureClientAsync(
            provider.GetRequiredService<IClientAdmin>(),
            CreateInteractiveClient(sampleBaseUrl),
            ct);
    }

    private static async Task EnsureApiScopeAsync(IApiScopeAdmin admin, CancellationToken ct)
    {
        if ((await admin.GetByNameAsync("sample-api", ct)).Found)
        {
            return;
        }

        var result = await admin.CreateAsync(
            new CreateApiScope
            {
                Name = "sample-api",
                DisplayName = "Storage sample API"
            },
            ct);

        EnsureSuccess(result, "sample-api scope");
    }

    private static async Task EnsureIdentityResourceAsync(
        IIdentityResourceAdmin admin,
        string name,
        List<string> userClaims,
        CancellationToken ct)
    {
        if ((await admin.GetByNameAsync(name, ct)).Found)
        {
            return;
        }

        var result = await admin.CreateAsync(
            new CreateIdentityResource
            {
                Name = name,
                DisplayName = name,
                UserClaims = userClaims
            },
            ct);

        EnsureSuccess(result, $"{name} identity resource");
    }

    private static async Task EnsureClientAsync(
        IClientAdmin admin,
        CreateClient client,
        CancellationToken ct)
    {
        if ((await admin.GetByClientIdAsync(client.ClientId, ct)).Found)
        {
            return;
        }

        var result = await admin.CreateAsync(client, ct);
        EnsureSuccess(result, $"{client.ClientId} client");
    }

    private static CreateClient CreateMachineClient()
    {
        var client = new CreateClient
        {
            ClientId = ClientCredentialsClientId,
            ClientName = "Storage sample machine client",
            AllowedGrantTypes = [GrantType.ClientCredentials],
            AllowedScopes = ["sample-api"],
            ClientSecrets =
            [
                new CreateClientSecret
                {
                    PlaintextValue = ClientSecret,
                    HashAlgorithm = SecretHashAlgorithm.Sha256
                }
            ]
        };
        client.ExtendedProperties.Set(AllowedRegionAttribute, AllowedRegion);
        return client;
    }

    private static CreateClient CreateInteractiveClient(string sampleBaseUrl)
    {
        var client = new CreateClient
        {
            ClientId = InteractiveClientId,
            ClientName = "Storage sample interactive client",
            RequireClientSecret = false,
            RequireConsent = false,
            AllowedGrantTypes = [GrantType.AuthorizationCode],
            AllowedScopes = ["openid", "profile", "sample-api"],
            RedirectUris = [$"{sampleBaseUrl}/sample/callback"]
        };
        client.ExtendedProperties.Set(AllowedRegionAttribute, AllowedRegion);
        return client;
    }

    private static void EnsureSuccess<TId>(SaveResult<TId> result, string item)
        where TId : notnull
    {
        if (result.IsSuccess)
        {
            return;
        }

        var errors = result.Errors is null
            ? "No error details were returned."
            : string.Join(Environment.NewLine, result.Errors.Select(error => $"{error.Code}: {error.Message}"));

        throw new InvalidOperationException($"Could not create {item}.{Environment.NewLine}{errors}");
    }
}
