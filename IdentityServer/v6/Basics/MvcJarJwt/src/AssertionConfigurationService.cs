// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Client;

public class AssertionConfigurationService : DefaultTokenClientConfigurationService
{
    private readonly AssertionService _assertionService;

    public AssertionConfigurationService(
        UserAccessTokenManagementOptions userAccessTokenManagementOptions,
        ClientAccessTokenManagementOptions clientAccessTokenManagementOptions,
        IOptionsMonitor<OpenIdConnectOptions> oidcOptions, IAuthenticationSchemeProvider schemeProvider,
        ILogger<AssertionConfigurationService> logger,
        AssertionService assertionService)

        : base(
            userAccessTokenManagementOptions,
            clientAccessTokenManagementOptions,
            oidcOptions,
            schemeProvider,
            logger)
    {
        _assertionService = assertionService;
    }

    protected override Task<ClientAssertion> CreateAssertionAsync(string clientName = null)
    {
        var assertion = new ClientAssertion
        {
            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
            Value = _assertionService.CreateClientToken()
        };

        return Task.FromResult(assertion);
    }


}
