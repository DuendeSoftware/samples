// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Duende.IdentityModel;

namespace IdentityServerHost;

public class TokenExchangeGrantValidator : IExtensionGrantValidator
{
    private readonly ITokenValidator _validator;

    public TokenExchangeGrantValidator(ITokenValidator validator)
    {
        _validator = validator;
    }

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        // defaults
        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest);
        var customResponse = new Dictionary<string, object>
        {
            {OidcConstants.TokenResponse.IssuedTokenType, OidcConstants.TokenTypeIdentifiers.AccessToken}
        };

        var subjectToken = context.Request.Raw.Get(OidcConstants.TokenRequest.SubjectToken);
        var subjectTokenType = context.Request.Raw.Get(OidcConstants.TokenRequest.SubjectTokenType);

        // mandatory parameters
        if (string.IsNullOrWhiteSpace(subjectToken))
        {
            return;
        }

        if (!string.Equals(subjectTokenType, OidcConstants.TokenTypeIdentifiers.AccessToken))
        {
            return;
        }

        var validationResult = await _validator.ValidateAccessTokenAsync(subjectToken);
        if (validationResult.IsError)
        {
            return;
        }

        var sub = validationResult.Claims.First(c => c.Type == JwtClaimTypes.Subject).Value;
        var clientId = validationResult.Claims.First(c => c.Type == JwtClaimTypes.ClientId).Value;

        var style = context.Request.Raw.Get("exchange_style");

        if (style == "impersonation")
        {
            // set token client_id to original id
            context.Request.ClientId = clientId;

            context.Result = new GrantValidationResult(
                subject: sub,
                authenticationMethod: GrantType,
                customResponse: customResponse);
        }
        else if (style == "delegation")
        {
            // set token client_id to original id
            context.Request.ClientId = clientId;

            var actor = new
            {
                client_id = context.Request.Client.ClientId
            };

            var actClaim = new Claim(JwtClaimTypes.Actor, JsonSerializer.Serialize(actor), IdentityServerConstants.ClaimValueTypes.Json);

            context.Result = new GrantValidationResult(
                subject: sub,
                authenticationMethod: GrantType,
                claims: new[] { actClaim },
                customResponse: customResponse);
        }
        else if (style == "custom")
        {
            context.Result = new GrantValidationResult(
                subject: sub,
                authenticationMethod: GrantType,
                customResponse: customResponse);
        }
    }

    public string GrantType => OidcConstants.GrantTypes.TokenExchange;
}
