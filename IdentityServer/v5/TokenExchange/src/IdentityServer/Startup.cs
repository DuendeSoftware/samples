// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerHost
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services.AddIdentityServer()
                .AddInMemoryApiScopes(Config.Scopes)
                .AddInMemoryClients(Config.Clients);

            // registers extension grant validator for the token exchange grant type
            builder.AddExtensionGrantValidator<TokenExchangeGrantValidator>();

            // register a profile service to emit the act claim
            builder.AddProfileService<ProfileService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseIdentityServer();
        }
    }
}
