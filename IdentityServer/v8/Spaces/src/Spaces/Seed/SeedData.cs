// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Admin;
using Duende.IdentityServer.Admin.ApiScopes;
using Duende.IdentityServer.Admin.Clients;
using Duende.IdentityServer.Admin.IdentityProviders;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores.Storage;
using Duende.Spaces;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;

namespace Spaces.Seed;

public class SeedData(IServiceProvider services)
{
    ISpaceAdmin spaceAdmin = services.GetRequiredService<ISpaceAdmin>();

    public async Task Seed(CancellationToken ct)
    {
        var space1 = await CreateSpace("space1", matchOrigin: "https://space1.dev.localhost:5001", ct: ct);
        var space2 = await CreateSpace("space2", matchOrigin: "https://space2.dev.localhost:5001", ct: ct);
        var space3 = await CreateSpace("space3", matchPath: "/space3", ct: ct);

        SpaceId[] spaces = [SpaceId.Default, space1, space2, space3];

        // Seed users in each space. Note, space3 doesn't have a user.
        await CreateUser(SpaceId.Default, DemoCredentials.GetUsernameForSpace("default")!, ct);
        await CreateUser(space1, DemoCredentials.GetUsernameForSpace("space1")!, ct);
        await CreateUser(space2, DemoCredentials.GetUsernameForSpace("space2")!, ct);

        // All user data in each space is completely isolated from each other.
        // So, the same username can be used in each space.
        foreach (var space in spaces)
        {
            await CreateUser(space, "example-user", ct);
        }

        // Create an idp for each space that points to demo.dudendesoftware.com
        await CreateIdp(SpaceId.Default, "default-idp", ct);
        await CreateIdp(space1, "space1-idp", ct);
        await CreateIdp(space2, "space2-idp", ct);
        await CreateIdp(space3, "space3-idp", ct);

        // Create a unique client in each space.
        // API scopes must be created once per space before any clients reference them.
        foreach (var space in spaces)
        {
            await CreateApiScope(space, "scope1", ct);
        }

        await CreateClient(SpaceId.Default, "default-client", ct);
        await CreateClient(space1, "space1-client", ct);
        await CreateClient(space2, "space2-client", ct);
        await CreateClient(space3, "space3-client", ct);

        // All configuration data in each space is completely isolated. So if we want
        // the same client in all spaces, we have to explicitly create it.
        foreach (var space in spaces)
        {
            await CreateClient(space, "example-client", ct);
        }
    }
    private async Task CreateIdp(SpaceId spaceId, string name, CancellationToken ct)
    {
        var spacedService = services.GetServiceForSpace<IIdentityProviderAdmin>(spaceId);
        var schemaStore = services.GetRequiredService<ISchemaStore>();

        var oidcSchema = await schemaStore.GetAsync(SchemaId.IdentityProvider("oidc"), ct);
        var values = new AttributeValueCollection(oidcSchema);
        values.Set(OidcProviderSchema.Authority, "https://demo.duendesoftware.com");
        values.Set(OidcProviderSchema.ClientId, "interactive.confidential");
        values.Set(OidcProviderSchema.ClientSecret, "secret");
        values.Set(OidcProviderSchema.ResponseType, "code");
        values.Set(OidcProviderSchema.GetClaimsFromUserInfoEndpoint, "true");

        var saveResult = await spacedService.Service.CreateAsync(new CreateIdentityProvider()
        {
            Scheme = "oidc",
            Type = "oidc",
            Enabled = true,
            DisplayName = name,
            ExtendedProperties = values
        }, ct);

        if (!saveResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to save identity provider. {saveResult}");
        }
    }

    private async Task<SpaceId> CreateSpace(string name, string? matchOrigin = null, PathString? matchPath = null,
        CancellationToken ct = default)
    {
        var saveResult = await spaceAdmin.CreateAsync(new CreateSpaceConfiguration
        {
            Name = name,
            MatchPatterns =
            [
                new SpaceMatchPattern()
                {
                    Origin = matchOrigin,
                    Path = matchPath?.ToString()
                }
            ],
        }, ct);

        if (!saveResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to save space {name}");
        }

        return saveResult.Id;
    }

    private async Task CreateUser(SpaceId spaceId, string userName, CancellationToken ct)
    {
        using var spacedUserAdmin = services.GetServiceForSpace<IUserAdmin>(spaceId);
        var profileAdmin = spacedUserAdmin.Service.Profiles;

        // First, we'll need to create a profile for the user. It has a unique username.
        var attributes = new AttributeValueCollection(await profileAdmin.GetSchemaAsync(ct));
        attributes.Set(DemoUserAttributes.UserName, userName); // This username is used for username / password login. 
        var userSubjectId = UserSubjectId.New();
        var saved = await profileAdmin.TryAddAsync(userSubjectId, attributes.Validate(), ct);

        if (saved == null)
        {
            throw new InvalidOperationException("Failed to save user");
        }

        // Then we have to setup password authentication. To do this, we first have to add an (empty) authenticator. 
        await spacedUserAdmin.Service.Authenticators.TryAddAsync(userSubjectId, [], [], ct);

        // Then we can set the password on the user. To do this, we have to create
        // a validated password (to see if it matches the password policies)
        using var spacedAuthenticatorSelfService = services.GetServiceForSpace<IUserAuthenticatorsSelfService>(spaceId);
        var validatedPassword = await spacedAuthenticatorSelfService.Service.ValidatePasswordAsync(userSubjectId, DemoCredentials.Password, ct);

        if (validatedPassword == null)
        {
            throw new InvalidOperationException("password doesn't comply with the password policy");
        }

        var setPasswordResult = await spacedAuthenticatorSelfService.Service.TrySetPasswordAsync(userSubjectId, validatedPassword, ct);

        if (!setPasswordResult)
        {
            throw new InvalidOperationException($"Failed to set password. {setPasswordResult}");
        }
    }

    private async Task CreateApiScope(SpaceId space, string scopeName, CancellationToken ct)
    {
        using var scopeAdmin = services.GetServiceForSpace<IApiScopeAdmin>(space);

        var saveResult = await scopeAdmin.Service.CreateAsync(new CreateApiScope()
        {
            Name = scopeName,
            Enabled = true
        }, ct);

        if (!saveResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to save apiScope '{scopeName}' in space {space}");
        }
    }

    private async Task CreateClient(SpaceId space, string clientId, CancellationToken ct)
    {
        using var clientAdmin = services.GetServiceForSpace<IClientAdmin>(space);

        var saveResult = await clientAdmin.Service.CreateAsync(
            new CreateClient()
            {
                ClientId = clientId,
                AllowedGrantTypes = [GrantType.ClientCredentials],
                AllowedScopes = ["scope1"],
                ClientSecrets =
                [
                    new CreateClientSecret()
                    {
                        PlaintextValue = "secret",
                        HashAlgorithm = SecretHashAlgorithm.Sha256
                    }
                ]
            }, ct);

        if (!saveResult.IsSuccess)
        {
            throw new InvalidOperationException("Failed to save client");
        }
    }
}
