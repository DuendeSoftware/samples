// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;

namespace IdentityServerHost.WsFed
{
    public class EfWsFedProviderStore : IdentityProviderStore
    {
        public EfWsFedProviderStore(IConfigurationDbContext context, ILogger<IdentityProviderStore> logger) : base(context, logger)
        {
        }

        protected override IdentityProvider MapIdp(Duende.IdentityServer.EntityFramework.Entities.IdentityProvider idp)
        {
            var result = base.MapIdp(idp);

            if (result == null && idp.Type == "wsfed")
            {
                result = new WsFedProvider(idp.ToModel());
            }

            return result;
        }
    }
}
