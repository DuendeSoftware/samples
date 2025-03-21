// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Client
{
    public class AssertionConfigurationService : DefaultTokenClientConfigurationService
    {
        private readonly AssertionService _assertionService;

        public AssertionConfigurationService(
            IOptions<AccessTokenManagementOptions> accessTokenManagementOptions,
            IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
            IAuthenticationSchemeProvider schemeProvider,
            AssertionService assertionService) : base(accessTokenManagementOptions,
            oidcOptions,
            schemeProvider)
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
}
