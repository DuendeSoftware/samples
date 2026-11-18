// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtBearerValidator(IOptionsFactory<JwtBearerOptions> jwtBearerOptions)
{
    private readonly JwtBearerOptions _jwtOptions = jwtBearerOptions.Create(JwtBearerDefaults.AuthenticationScheme);

    // Inject the validator and the configured options
    // Retrieve the TokenValidationParameters from the options
    // We use the scheme name "Bearer" to get the correct options.

    public async Task<(ClaimsPrincipal Principal, SecurityToken Token)?> ValidateToken(string token)
    {
        // The token often comes with "Bearer " prefix, which needs to be removed.
        var tokenToValidate = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token.Substring(7)
            : token;

        var openIdConfig = await _jwtOptions.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
        

        try
        {
            var tokenValidator = new JwtSecurityTokenHandler();
            // Validate the token using the injected validator and parameters.
            // This method throws an exception if the token is invalid.
            // 2. Clone the TokenValidationParameters from your options.
            var validationParameters = _jwtOptions.TokenValidationParameters.Clone();

            // 3. Apply the discovered issuer and signing keys.
            validationParameters.ValidIssuer = openIdConfig.Issuer;
            validationParameters.IssuerSigningKeys = openIdConfig.SigningKeys;

            // 4. Validate the token using the fully populated parameters.
            var claimsPrincipal = tokenValidator.ValidateToken(
                tokenToValidate,
                validationParameters,
                out SecurityToken validatedToken);

            return (claimsPrincipal, validatedToken);
        }
        catch (SecurityTokenException ex)
        {
            // Token validation failed (e.g., expired, invalid signature, etc.)
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Some other error occurred
            Console.WriteLine($"An error occurred: {ex.Message}");
            return null;
        }
    }
}
