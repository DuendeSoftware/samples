// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Duende.IdentityModel.Jwk;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace CIMD.IdentityServer;

/// <summary>
/// Creates the IdentityServer Client model from a CIMD document and optional JWKS
/// </summary>
public static class CimdClientBuilder
{
    private static readonly JsonSerializerOptions JwkSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
    };

    public static Client Build(
        string clientId,
        CimdDocument document,
        JsonWebKeySet? keySet)
    {
        var scopes = document.Scope?.Split(' ').ToList() ?? [];
        var allowOfflineAccess = scopes.Contains("offline_access");
        scopes.Remove("offline_access");

        var client = new Client
        {
            ClientId = clientId,
            ClientName = document.ClientName,
            LogoUri = document.LogoUri?.ToString(),
            ClientUri = document.ClientUri?.ToString(),
            RedirectUris = document.RedirectUris?.Select(u => u.ToString()).ToList() ?? [],
            PostLogoutRedirectUris = document.PostLogoutRedirectUris?.Select(u => u.ToString()).ToList() ?? [],
            AllowedGrantTypes = document.GrantTypes?.ToList() ?? GrantTypes.Code,
            RequireClientSecret = keySet is not null,
            AllowedScopes = scopes,
            AllowOfflineAccess = allowOfflineAccess
        };

        if (keySet is not null)
        {
            foreach (var key in keySet.Keys)
            {
                var jwk = JsonSerializer.Serialize(key, JwkSerializerOptions);
                client.ClientSecrets.Add(new Secret
                {
                    Type = IdentityServerConstants.SecretTypes.JsonWebKey,
                    Value = jwk
                });
            }
        }

        return client;
    }
}
