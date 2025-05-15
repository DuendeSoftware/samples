// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using Duende.IdentityServer.Configuration.Models;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;
using Duende.IdentityModel;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Configuration;

public class SoftwareStatementValidator : DynamicClientRegistrationValidator
{
    public SoftwareStatementValidator(ILogger<DynamicClientRegistrationValidator> logger) : base(logger)
    {
    }

    protected override Task<IStepResult> ValidateSoftwareStatementAsync(DynamicClientRegistrationContext context)
    {
        var rawSoftwareStatement = context.Request.SoftwareStatement;
        if (string.IsNullOrEmpty(rawSoftwareStatement))
        {
            return StepResult.Success();
        }

        var handler = new JsonWebTokenHandler();

        // This sample uses hard-coded public and private RSA keys to sign and
        // validate the software statement. A real implementation might share
        // RSA keys through other means.
        var rsa = new RSAParameters
        {
            Exponent = Convert.FromBase64String("AQAB"),
            Modulus = Convert.FromBase64String(
                "rTLn01iUFgUAX7Cl6nFjE3FnegQP6jCPq2qffhLw50ZrAgkZdPz2ITE7DCjJL4Ln9YLpldCIZhqImHz3ojfMD2Yuf2ac6H1l96ZyIVqxTrm7fIagGhbJjzrxBRGQIYRawMVmWMo0vksuWM0U5lImdLbL4j74soRjg2QgTqomAvWqHcOSOBnIf5RfcUXHZCbsjZ8DAMUijR+Bjb8PqTq98UFiqLEDWUmz6qLOiO0aOV1VBBls6TuKlS+xJ/HNHbABbVIUewzdWsRKKiAUmQB5rU9InGZ8+B+OBl+dYDgaTruOe4R5dBfGRfeIkLjSQ2o55TfqVp/mSXDM0aSXBrrtzQ=="),
        };
        var key = new RsaSecurityKey(rsa)
        {
            KeyId = "SampleSoftwareStatementKey/1"
        };

        var parms = new TokenValidationParameters
        {
            IssuerSigningKey = key,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = false
        };

        var validateResult = handler.ValidateToken(rawSoftwareStatement, parms);
        if (validateResult.IsValid)
        {
            // Here, you should set client metadata values based on claims in
            // the software statement. As per rfc7591, metadata can be "directly
            // in the body of a registration request", "included as claims in a
            // software statement", or "a mixture of both", with the claims in
            // the software statement taking precedence.

            if (validateResult.Claims.ContainsKey(OidcConstants.ClientMetadata.SoftwareStatement))
            {
                context.Request.SoftwareId = validateResult.Claims[OidcConstants.ClientMetadata.SoftwareStatement].ToString();
            }

            if (validateResult.Claims.ContainsKey(OidcConstants.ClientMetadata.ClientName))
            {
                context.Request.ClientName = validateResult.Claims[OidcConstants.ClientMetadata.ClientName].ToString();
            }

            return StepResult.Success();
        }
        else
        {
            return StepResult.Failure("Invalid software statement", "invalid_software_statement");
        }


    }
}
