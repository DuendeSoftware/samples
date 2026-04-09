// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;

namespace Client;

public class ClientAssertionService(AssertionService assertionService) : IClientAssertionService
{
    public Task<ClientAssertion> GetClientAssertionAsync(ClientCredentialsClientName? clientName = null,
        TokenRequestParameters parameters = null,
        CancellationToken ct = new CancellationToken())
    {
        var assertion = new ClientAssertion
        {
            Type = OidcConstants.ClientAssertionTypes.JwtBearer,
            Value = assertionService.CreateClientToken()
        };

        return Task.FromResult(assertion);
    }
}
