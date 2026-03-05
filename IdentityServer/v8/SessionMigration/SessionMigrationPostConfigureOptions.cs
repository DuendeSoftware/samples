// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace SessionMigration;

public class SessionMigrationPostConfigureOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public SessionMigrationPostConfigureOptions(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public void PostConfigure(string name, CookieAuthenticationOptions options)
    {
        if (name != IdentityServerConstants.DefaultCookieAuthenticationScheme)
        {
            return;
        }

        if (options.TicketDataFormat == null)
        {
            throw new InvalidOperationException(
                "The session migration post configure relies on a TicketDataFormat being present. It is usually created by the default PostConfigureCookieOptions. Make sure the SessionMigrationPostConfigureOptions is added after the call to AddIdentityServer.");
        }

        // TicketDataFormat is not injected through DI so we have to supply it with an http context accessor
        // to be able to resolve request services at runtime.
        options.TicketDataFormat = new MigratingTicketDataFormat(httpContextAccessor, options, name);
    }
}
