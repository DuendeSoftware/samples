// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IdentityServerHost;

/// <summary>
/// Add extension methods for configuring WsFed dynamic providers.
/// </summary>
public static class IdentityServerBuilderWsFedExtensions
{
    /// <summary>
    /// Adds the WsFed dynamic provider feature.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddWsFedDynamicProvider(this IIdentityServerBuilder builder)
    {
        builder.Services.Configure<IdentityServerOptions>(options =>
        {
            // this associates the auth handler and options classes
            // to the idp class and type value from the identity provider store
            options.DynamicProviders.AddProviderType<WsFederationHandler, WsFederationOptions, WsFedProvider>("wsfed");
        });

        // this registers the configure to build the options from the provider data
        builder.Services.AddSingleton<IConfigureOptions<WsFederationOptions>, WsFedConfigureOptions>();

        // these are services from ASP.NET Core and are added manually since we're not using the 
        // AddWsFed helper that we'd normally use statically on the AddAuthentication.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<WsFederationOptions>, WsFederationPostConfigureOptions>());
        builder.Services.TryAddTransient<WsFederationHandler>();

        return builder;
    }

    /// <summary>
    /// Adds the in memory wsfed provider store.
    /// This API is for testing when you don't yet have a database for the provider data.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="providers"></param>
    /// <returns></returns>
    internal static IIdentityServerBuilder AddInMemoryWsFedProviders(this IIdentityServerBuilder builder, IEnumerable<WsFedProvider> providers)
    {
        builder.Services.AddSingleton(providers);
        builder.AddIdentityProviderStore<InMemoryWsFedProviderStore>();
        return builder;
    }
}
