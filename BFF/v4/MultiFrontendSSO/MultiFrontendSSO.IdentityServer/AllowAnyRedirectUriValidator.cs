// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

namespace MultiFrontendSSO.IdentityServer;

public class AllowAnyRedirectUriValidator : IRedirectUriValidator
{
    public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client) =>
        Task.FromResult(true);

    public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client) =>
        Task.FromResult(true);
}
