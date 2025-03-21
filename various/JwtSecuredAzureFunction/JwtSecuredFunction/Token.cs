// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace JwtSecuredFunction;

public static class Token
{
    private static readonly IConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager;
    private static string Authority = "https://demo.duendesoftware.com";

    static Token()
    {
        var documentRetriever = new HttpDocumentRetriever();

        ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{Authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            documentRetriever
        );
    }

    public static async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        var handler = new JsonWebTokenHandler();

        var config = await ConfigurationManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameter = new TokenValidationParameters()
        {
            ValidIssuer = Authority,
            ValidAudience = "api",
            IssuerSigningKeys = config.SigningKeys
        };

        return handler.ValidateToken(token, validationParameter);
    }

    public static async Task<ClaimsIdentity> ValidateAsync(HttpHeadersCollection headers, ILogger logger)
    {
        var found = headers.TryGetValues("Authorization", out var headerValues);
        if (!found)
        {
            logger.LogInformation("No authorization header found.");
            return null;
        }

        var values = headerValues.First().Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
        if (values?.Length != 2 || values?[0] != "Bearer")
        {
            logger.LogInformation("Invalid authorization header.");
            return null;
        }

        var result = await ValidateTokenAsync(values[1]);

        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            logger.LogInformation("Trying to refresh keys.");

            ConfigurationManager.RequestRefresh();

            result = await ValidateTokenAsync(values[1]);
        }

        if (result.IsValid)
        {
            logger.LogInformation("Valid token, returning identity.");
            return result.ClaimsIdentity;
        }

        logger.LogInformation("invalid token.");
        return null;
    }
}
