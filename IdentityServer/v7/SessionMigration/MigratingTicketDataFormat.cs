// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SessionMigration;
public class MigratingTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ISecureDataFormat<AuthenticationTicket> inner;
    private CookieAuthenticationOptions options;
    private readonly string scheme;

    // Copied from Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler,
    // unfortunatley it's private and cannot be referenced.
    private const string SessionIdClaim = "Microsoft.AspNetCore.Authentication.Cookies-SessionId";

    public MigratingTicketDataFormat(
        IHttpContextAccessor httpContextAccessor,
        CookieAuthenticationOptions options,
        string scheme)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.options = options;
        this.scheme = scheme;

        // Capture the inner at construction as the value in options will be replaced with
        // a reference to this instance.
        inner = options.TicketDataFormat;
    }

    public string Protect(AuthenticationTicket data) => inner.Protect(data);
    public string Protect(AuthenticationTicket data, string purpose) => inner.Protect(data, purpose);
    public AuthenticationTicket Unprotect(string protectedText) => inner.Unprotect(protectedText);
    public AuthenticationTicket Unprotect(string protectedText, string purpose)
    {
        var ticket = inner.Unprotect(protectedText, purpose);

        if (ticket.Principal.HasClaim(c => c.Type == SessionIdClaim))
        {
            // The ticket is already a reference ticket into the session store, just return it.
            return ticket;
        }

        var context = httpContextAccessor.HttpContext;
        var sessionStore = context.RequestServices.GetRequiredService<IServerSideTicketStore>();

        // Unprotect isn't async so we'll have block the thread and wait using .Result when calling an
        // async methods. This is bad for performance, but this only happens once per logged in user
        // session so it should be acceptable. Thanks to Asp.Net Core not having any synchronization
        // context there is no risk for dead locks.
        var sessionId = sessionStore.StoreAsync(ticket).Result;

        // There's a potential race condition where two requests could migrate the same session. Check
        // if there's another entry with the same SID and if it is rollback the one we created and
        // don't alter the cookie.
        if (HasDuplicate(sessionStore, ticket))
        {
            sessionStore.RemoveAsync(sessionId).Wait();
        }
        else
        {
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(SessionIdClaim, sessionId, ClaimValueTypes.String, options.ClaimsIssuer) },
                    options.ClaimsIssuer));

            ticket = new AuthenticationTicket(principal, null, scheme);

            var cookieValue = inner.Protect(ticket, purpose);

            var cookieOptions = CreateCookieOptions(ticket, context);

            options.CookieManager.AppendResponseCookie(
                context,
                options.Cookie.Name!,
                cookieValue,
                cookieOptions);
        }
        return ticket;
    }

    private CookieOptions CreateCookieOptions(AuthenticationTicket ticket, HttpContext context)
    {
        // Cookie option generation copied from cookie handler.

        var cookieOptions = options.Cookie.Build(context);
        cookieOptions.Expires = null;

        if (ticket.Properties.IsPersistent)
        {
            DateTimeOffset issuedUtc;
            if (ticket.Properties.IssuedUtc.HasValue)
            {
                issuedUtc = ticket.Properties.IssuedUtc.Value;
            }
            else
            {
                issuedUtc = DateTime.UtcNow;
                ticket.Properties.IssuedUtc = issuedUtc;
            }

            if (!ticket.Properties.ExpiresUtc.HasValue)
            {
                ticket.Properties.ExpiresUtc = issuedUtc.Add(options.ExpireTimeSpan);
            }

            var expiresUtc = ticket.Properties.ExpiresUtc ?? issuedUtc.Add(options.ExpireTimeSpan);
            cookieOptions.Expires = expiresUtc.ToUniversalTime();
        }

        return cookieOptions;
    }

    private bool HasDuplicate(IServerSideTicketStore sessionStore, AuthenticationTicket ticket)
    {
        var sid = ticket.GetSessionId();

        var filter = new SessionQuery
        {
            SessionId = sid
        };

        var sessions = sessionStore.QuerySessionsAsync(filter).Result;

        // There should be only one entry, the one we just created.
        return sessions.Results.Count > 1;
    }
}
