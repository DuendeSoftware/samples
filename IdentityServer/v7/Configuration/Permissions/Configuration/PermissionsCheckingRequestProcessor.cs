// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.RequestProcessing;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace Configuration;

/// <summary>
/// This request processor can set the client secret, if it is supplied as a
/// property of the dynamic client registration request document. A special
/// scope is also 
/// </summary>
public class PermissionsCheckingRequestProcessor : DynamicClientRegistrationRequestProcessor
{
    private readonly ILogger<PermissionsCheckingRequestProcessor> _logger;

    public PermissionsCheckingRequestProcessor(IdentityServerConfigurationOptions options, IClientConfigurationStore store, ILogger<PermissionsCheckingRequestProcessor> logger)
        : base(options, store)
    {
        _logger = logger;
    }

    protected override async Task<(Secret, string)> GenerateSecret(DynamicClientRegistrationContext context)
    {
        if (context.Request.Extensions.TryGetValue("client_secret", out var secretParam))
        {
            // Remove the client_secret, so that we don't echo back a duplicate 
            // or inconsistent value
            context.Request.Extensions.Remove("client_secret");

            if (!context.Caller.HasClaim("scope", "IdentityServer.Configuration:SetClientSecret"))
            {
                _logger.LogWarning("The dynamic client request includes a secret, but the required IdentityServer.Configuration:SetClientSecret scope is missing. The secret is ignored.");
            }
            else
            {
                var plainText = secretParam.ToString();
                ArgumentNullException.ThrowIfNull(plainText);
                var secret = new Secret(plainText.ToSha256());

                return (secret, plainText);
            }
        }
        return await base.GenerateSecret(context);
    }
}
