// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace IdentityServerHost.WsFed;

public class InMemoryWsFedProviderStore : IIdentityProviderStore
{
    private readonly IEnumerable<WsFedProvider> _providers;

    public InMemoryWsFedProviderStore(IEnumerable<WsFedProvider> providers)
    {
        _providers = providers;
    }

    public Task<IEnumerable<IdentityProviderName>> GetAllSchemeNamesAsync()
    {
        return Task.FromResult(_providers.Select(x => new IdentityProviderName
        {
            DisplayName = x.DisplayName,
            Enabled = x.Enabled,
            Scheme = x.Scheme
        }));
    }

    public Task<IdentityProvider> GetBySchemeAsync(string scheme)
    {
        var provider = _providers.SingleOrDefault(x => x.Scheme == scheme);
        return Task.FromResult<IdentityProvider>(provider);
    }
}
