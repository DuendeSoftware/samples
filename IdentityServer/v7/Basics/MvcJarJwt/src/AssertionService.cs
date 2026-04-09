// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.IdentityModel;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Client;

public class AssertionService(IConfiguration configuration)
{
    public string CreateClientToken()
    {
        var now = DateTimeOffset.UtcNow;
        var clientId = configuration.GetValue<string>("ClientId");

        // in production you should load that key from some secure location
        var key = configuration.GetValue<string>("Secrets:Key");

        var token = new JwtSecurityToken(
            clientId,
            Urls.IdentityServer,
            new List<Claim>()
            {
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                new Claim(JwtClaimTypes.Subject, clientId),
                new Claim(JwtClaimTypes.IssuedAt, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            now.UtcDateTime,
            now.UtcDateTime.AddMinutes(1),
            new SigningCredentials(new JsonWebKey(key), "RS256")
        );

        token.Header[JwtClaimTypes.TokenType] = "client-authentication+jwt";

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        return tokenHandler.WriteToken(token);
    }

    public string SignAuthorizationRequest(OpenIdConnectMessage message)
    {
        var now = DateTime.UtcNow;
        var clientId = configuration.GetValue<string>("ClientId");

        // in production you should load that key from some secure location
        var key = configuration.GetValue<string>("Secrets:Key");

        var claims = new List<Claim>();
        foreach (var parameter in message.Parameters)
        {
            claims.Add(new Claim(parameter.Key, parameter.Value));
        }

        var token = new JwtSecurityToken(
            clientId,
            Urls.IdentityServer,
            claims,
            now,
            now.AddMinutes(1),
            new SigningCredentials(new JsonWebKey(key), "RS256")
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();

        return tokenHandler.WriteToken(token);
    }
}
