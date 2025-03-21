// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;

namespace IdentityServerHost;

public class AcrDiscoveryDocumentGenerator : DiscoveryResponseGenerator
{
    public AcrDiscoveryDocumentGenerator(IdentityServerOptions options, IResourceStore resourceStore, IKeyMaterialService keys, ExtensionGrantValidator extensionGrants, ISecretsListParser secretParsers, IResourceOwnerPasswordValidator resourceOwnerValidator, ILogger<DiscoveryResponseGenerator> logger) : base(options, resourceStore, keys, extensionGrants, secretParsers, resourceOwnerValidator, logger)
    {
    }

    public override async Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri)
    {
        var result = await base.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);
        if (!result.ContainsKey(IdentityModel.OidcConstants.Discovery.AcrValuesSupported))
        {
            result.Add(IdentityModel.OidcConstants.Discovery.AcrValuesSupported, new[] { "1" });
        }
        return result;
    }
}
