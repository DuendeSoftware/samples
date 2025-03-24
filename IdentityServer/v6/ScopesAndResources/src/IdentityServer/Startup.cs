// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerHost;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = services.AddIdentityServer(options =>
        {
            // emits static audience if required
            options.EmitStaticAudienceClaim = false;

            // control format of scope claim
            options.EmitScopesAsSpaceDelimitedStringInJwt = true;
        })
            .AddInMemoryApiScopes(Config.Scopes)
            .AddInMemoryApiResources(Config.Resources)
            .AddInMemoryClients(Config.Clients);

        // registers the scope parser for the transaction scope
        builder.AddScopeParser<ParameterizedScopeParser>();

        // register the token request validator to access the parsed scope in the pipeline
        builder.AddCustomTokenRequestValidator<TokenRequestValidator>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseIdentityServer();
    }
}
