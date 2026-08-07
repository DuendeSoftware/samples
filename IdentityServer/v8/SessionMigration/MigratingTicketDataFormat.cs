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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecureDataFormat<AuthenticationTicket> _inner;
    private readonly CookieAuthenticationOptions _options;
    private readonly string _scheme;

    // Copied from Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler.
    // Unfortunately, it's private and cannot be referenced.
    private const string SessionIdClaim = "Microsoft.AspNetCore.Authentication.Cookies-SessionId";

    public MigratingTicketDataFormat(
        IHttpContextAccessor httpContextAccessor,
        CookieAuthenticationOptions options,
        string scheme)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _scheme = scheme;

        // Capture the inner at construction as the value in options will be replaced with
        // a reference to this instance.
        _inner = options.TicketDataFormat;
    }

    public string Protect(AuthenticationTicket data) => _inner.Protect(data);
    public string Protect(AuthenticationTicket data, string purpose) => _inner.Protect(data, purpose);
    public AuthenticationTicket Unprotect(string protectedText) => _inner.Unprotect(protectedText);
    public AuthenticationTicket Unprotect(string protectedText, string purpose)
    {
        var ticket = _inner.Unprotect(protectedText, purpose);

        if (ticket.Principal.HasClaim(c => c.Type == SessionIdClaim))
        {
            // The ticket is already a reference ticket into the session store, just return it.
            return ticket;
        }

        var context = _httpContextAccessor.HttpContext;
        var sessionStore = context.RequestServices.GetRequiredService<IServerSideTicketStore>();

        // Unprotect isn't async so we have to block synchronously. ISecureDataFormat<T> is a
        // synchronous interface defined by ASP.NET Core and cannot be changed. Thanks to ASP.NET
        // Core not having a synchronization context there is no risk for deadlocks.
        //
        // NOTE: In a real implementation with many concurrent users migrating at once (e.g. after
        // a deployment), blocking here could still cause thread pool starvation. Consider whether your
        // scale warrants an alternative approach, such as splitting the migration into two phases:
        //   1. Detect the old cookie-based session in Unprotect and flag the request (e.g. via
        //      HttpContext.Items) without doing any I/O.
        //   2. In async middleware further up the pipeline, check the flag, perform the actual
        //      store write, and rewrite the cookie — all fully async.
        // This avoids sync-over-async entirely at the cost of more architectural complexity.
        var sessionId = sessionStore.StoreAsync(ticket).GetAwaiter().GetResult();

        // There's a potential race condition where two requests could migrate the same session.
        // Check if there's another entry with the same SID and if so, rollback the one we
        // created and don't alter the cookie.
        //
        // NOTE: This check has a TOCTOU (time-of-check-time-of-use) issue. Two concurrent
        // requests could both store a session, both detect the duplicate, and both roll back,
        // leaving no migrated session. This is self-healing (the next request will retry), but
        // in a real implementation you may want to use a distributed lock or compare creation
        // timestamps to decide which duplicate to keep.
        if (HasDuplicate(sessionStore, ticket))
        {
            sessionStore.RemoveAsync(sessionId).GetAwaiter().GetResult();
        }
        else
        {
            // Generate a new AuthenticationTicket to return the new session cookie in the HTTP response
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(SessionIdClaim, sessionId, ClaimValueTypes.String, _options.ClaimsIssuer) },
                    _options.ClaimsIssuer));

            var newTicket = new AuthenticationTicket(principal, null, _scheme);
            var cookieValue = _inner.Protect(newTicket, purpose);

            // Reuse the properties from the original ticket to get the right cookie options (IsPersistent).
            var cookieOptions = CreateCookieOptions(ticket, context);

            // NOTE: AppendResponseCookie will throw if the response has already started
            // (e.g. if headers have been sent). In a real implementation, guard against this
            // or ensure migration runs early enough in the pipeline.
            _options.CookieManager.AppendResponseCookie(
                context,
                _options.Cookie.Name!,
                cookieValue,
                cookieOptions);
        }
        return ticket;
    }

    private CookieOptions CreateCookieOptions(AuthenticationTicket ticket, HttpContext context)
    {
        // Cookie option generation copied from cookie handler.

        var cookieOptions = _options.Cookie.Build(context);
        cookieOptions.Expires = null;

        if (ticket.Properties.IsPersistent)
        {
            var issuedUtc = ticket.Properties.IssuedUtc ?? DateTimeOffset.UtcNow;
            var expiresUtc = ticket.Properties.ExpiresUtc ?? issuedUtc.Add(_options.ExpireTimeSpan);
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

        var sessions = sessionStore.QuerySessionsAsync(filter, CancellationToken.None)
            .GetAwaiter().GetResult();

        // There should be only one entry, the one we just created.
        return sessions.Results.Count > 1;
    }
}
