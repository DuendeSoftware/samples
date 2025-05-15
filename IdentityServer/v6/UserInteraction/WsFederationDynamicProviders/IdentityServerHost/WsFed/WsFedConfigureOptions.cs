// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Hosting.DynamicProviders;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityServerHost.WsFed;

class WsFedConfigureOptions : ConfigureAuthenticationOptions<WsFederationOptions, WsFedProvider>
{
    public WsFedConfigureOptions(IHttpContextAccessor httpContextAccessor, ILogger<ConfigureAuthenticationOptions<WsFederationOptions, WsFedProvider>> logger) : base(httpContextAccessor, logger)
    {
    }

    protected override void Configure(ConfigureAuthenticationContext<WsFederationOptions, WsFedProvider> context)
    {
        context.AuthenticationOptions.SignInScheme = context.DynamicProviderOptions.SignInScheme;
        context.AuthenticationOptions.SignOutScheme = context.DynamicProviderOptions.SignOutScheme;

        context.AuthenticationOptions.MetadataAddress = context.IdentityProvider.MetadataAddress;
        context.AuthenticationOptions.RequireHttpsMetadata = context.IdentityProvider.MetadataAddress.StartsWith("https");

        context.AuthenticationOptions.Wtrealm = context.IdentityProvider.RelyingPartyId;
        context.AuthenticationOptions.AllowUnsolicitedLogins = context.IdentityProvider.AllowIdpInitiated;

        context.AuthenticationOptions.TokenValidationParameters.NameClaimType = JwtClaimTypes.Name;
        context.AuthenticationOptions.TokenValidationParameters.RoleClaimType = JwtClaimTypes.Role;

        context.AuthenticationOptions.CallbackPath = context.PathPrefix;
        context.AuthenticationOptions.RemoteSignOutPath = context.PathPrefix;

        context.AuthenticationOptions.Events.OnRedirectToIdentityProvider = ctx =>
        {
            if (ctx.ProtocolMessage.IsSignOutMessage)
            {
                var url = ctx.HttpContext.Request.Scheme + "://" +
                    ctx.HttpContext.Request.Host +
                    ctx.HttpContext.Request.PathBase +
                    ctx.Options.CallbackPath;

                var identityServerOptions = ctx.HttpContext.RequestServices.GetRequiredService<IdentityServerOptions>();

                var uri = new Uri(ctx.ProtocolMessage.Wreply, UriKind.Absolute);
                if (uri.AbsolutePath.EndsWith(identityServerOptions.UserInteraction.LogoutUrl, StringComparison.OrdinalIgnoreCase) && uri.Query.Contains(identityServerOptions.UserInteraction.LogoutIdParameter + "="))
                {
                    url += uri.Query;
                }
                else
                {
                    // empty value to trigger logic in OnRemoteFailure below
                    url += "?" + identityServerOptions.UserInteraction.LogoutIdParameter + "=";
                }

                ctx.ProtocolMessage.Wreply = url;
            }
            return Task.CompletedTask;
        };

        context.AuthenticationOptions.Events.OnRemoteFailure = ctx =>
        {
            var identityServerOptions = ctx.HttpContext.RequestServices.GetRequiredService<IdentityServerOptions>();

            if (HttpMethods.IsGet(ctx.Request.Method) &&
                ctx.Request.Path == ctx.Options.CallbackPath &&
                ctx.Request.Query.ContainsKey(identityServerOptions.UserInteraction.LogoutIdParameter))
            {
                ctx.Response.Redirect(identityServerOptions.UserInteraction.LogoutUrl + "?" + identityServerOptions.UserInteraction.LogoutIdParameter + "=" + ctx.Request.Query[identityServerOptions.UserInteraction.LogoutIdParameter]);
                ctx.HandleResponse();
            }

            return Task.CompletedTask;
        };
    }
}
