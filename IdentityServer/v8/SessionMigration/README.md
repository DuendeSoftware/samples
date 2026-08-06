# Session Migration sample

## Instructions

The purpose of this sample is to show how to migrate existing client side session to server
side session without the user having to login again. To test it, first run the sample as is
and log in (user: alice, password: alice) to get a normal cookie based session in your
browser. Then uncomment the following blocks and run the sample again. The first request
from your browser to IdentityServer will cause the session to be migrated to be a server
side session.

Note that further restarts will invalidate your session as the server side session
store in this sample is in memory only.
If server side sessions have been enabled and then are removed before another
test run, you have to manually clear the cookie to be able to log in again.

This is the normal template code to activate server-side sessions:
```csharp
var isBuilder = builder.Services.AddIdentityServer(...);

isBuilder.AddServerSideSessions();
```

And this code adds migration of sessions. Enabling server-side sessions without adding this migration, will
invalidate all existing sessions.
```csharp
builder.Services.AddTransient<IPostConfigureOptions<CookieAuthenticationOptions>, SessionMigrationPostConfigureOptions>();
```
