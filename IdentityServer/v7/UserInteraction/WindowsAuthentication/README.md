# Windows authentication sample

This sample shows how to use **Windows Authentication** (Negotiate / Kerberos / NTLM) as the external
identity provider for IdentityServer, so users sign in with their existing Windows account and there
is no local user store at all.

### What the sample demonstrates

- configuring `IISOptions` with `iis.AuthenticationDisplayName = "Windows"` and
  `iis.AutomaticAuthentication = false` — the display name is what surfaces the scheme as an external
  provider on the login page, and turning automatic authentication off means the Windows challenge is
  triggered deliberately rather than on every request
- the login page at `/Account/Login` redirecting **unconditionally** to a dedicated
  `/Account/Login/Windows` Razor Page. The dynamic alternative — inspect the authorization context
  and redirect only when `context?.IdP == "Windows"` — is present in the same file, commented out,
  so you can switch between the two
- calling `HttpContext.AuthenticateAsync("Windows")` and inspecting the resulting `WindowsPrincipal`
- using the user's **SID** as the subject identifier:
  `new IdentityServerUser(wp.FindFirst(ClaimTypes.PrimarySid).Value)`
- translating the user's Windows **group** memberships into `role` claims with
  `WindowsIdentity.Groups.Translate(typeof(NTAccount))`, which the sample flags with a performance
  warning — loading group SIDs is expensive
- `Challenge("Windows")` for the first visit, because Windows authentication does not support a
  redirect URI; the same URL is re-entered on the way back
- forcing the IdP selection from the client by sending `acr_values=idp:Windows` in an
  `OnRedirectToIdentityProvider` handler, so the user never has to pick a provider
- a confidential client with `BackChannelLogoutUri`, so signing out propagates back to the client

> [!IMPORTANT]
> This sample has **hard platform requirements**. It only runs on **Windows**, and the IdentityServer
> host only works when run under **IIS or IIS Express** with the Windows Authentication provider
> enabled. `IdentityServerHost` has an `IISExpress` launch profile and **no** `Project` profile, so
> `dotnet run --project IdentityServerHost` will not give you a working sign-in. Running it under
> the Aspire AppHost (which uses `dotnet run` and plain Kestrel) will **not** provide Windows
> authentication either.

> **Note:** the `WindowsAuthentication.AppHost` project exists and will start the client, but it
> cannot make the token server work on its own. Treat the Aspire dashboard as optional here and
> start the IdentityServer host from Visual Studio (or any IIS Express host) instead.

## Prerequisites

- **Windows** — `IdentityServerHost` targets `net10.0-windows` and uses
  `System.Security.Principal.WindowsIdentity` / `WindowsPrincipal`.
- **IIS Express** (or IIS) with the ASP.NET Core Hosting Bundle installed and the **Windows
  Authentication** provider enabled (Negotiate and/or NTLM). The `IISExpress` launch profile in
  `IdentityServerHost/Properties/launchSettings.json` already sets `iisSettings.windowsAuthentication:
  true` and `anonymousAuthentication: true`.
- **Kerberos** additionally requires a domain-joined machine. A standalone or workgroup machine
  will fall back to NTLM.
- A browser that supports integrated Windows authentication — Edge (IE mode) or another browser
  configured to delegate to Windows. A normal Chrome or Firefox window will not complete the
  handshake silently.
- .NET 10 SDK and a trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## How to Run

1. Open `WindowsAuthentication.sln` in Visual Studio, right-click `IdentityServerHost`, choose
   **Properties → Run Debug** and confirm the profile is **IISExpress**, then press F5. It listens on
   `https://localhost:44324/`.
1. Start the client in a second terminal:

   ```bash
   dotnet run --project Client
   ```

   or use the Aspire AppHost for the client and the dashboard:

   ```bash
   dotnet run --project WindowsAuthentication.AppHost
   ```

   The Aspire dashboard is at `https://localhost:17247`.

1. Open a browser tab to the client at `https://localhost:44300`. Note that
   `app.MapDefaultControllerRoute().RequireAuthorization()` means **every** page in the client
   requires an authenticated user, so there is no anonymous landing page.
1. The client's OpenID Connect handler adds `acr_values=idp:Windows` and redirects to
   `https://localhost:44324/account/login`.
1. The login page immediately redirects to `/Account/Login/Windows` — **the local username and
   password form is never shown**. That page calls `Challenge("Windows")`, which triggers the
   Windows logon prompt or the integrated-authentication handshake.
1. After you authenticate, you land back on the client's `Secure` page. Look at the claims:
   - `sub` is your Windows **SID**
   - `name` is your Windows account name
   - the `role` claims are your **Windows group names** (for example `BUILTIN\Users` or
     `BUILTIN\Administrators`), translated from `WindowsIdentity.Groups`
1. Click **Logout** to see the back-channel logout round trip to the client's
   `https://localhost:44300/logout` endpoint.

There are no sample usernames or passwords to type — you authenticate as whichever Windows account
you are logged in to. A `TestUsers.cs` file exists in the project and the login page model
constructs a `TestUserStore` from it if one is not injected, but `AddTestUsers` is never called and
the local login path is unreachable while `OnGet` always redirects to the Windows page.

## Projects and URLs

| Project | URL | Notes |
|---------|-----|-------|
| `IdentityServerHost` | `https://localhost:44324/` | must be hosted by IIS / IIS Express |
| `Client` | `https://localhost:44300` | **open this one** |
| `WindowsAuthentication.AppHost` (Aspire dashboard) | `https://localhost:17247` | starts the client; the token server still needs IIS Express |

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/Pages/Account/Login/Windows.cshtml.cs` — the whole sample: group claims, the
  SID subject, and the Windows challenge
- `IdentityServerHost/Pages/Account/Login/Index.cshtml.cs` — the unconditional redirect to the
  Windows page, with the conditional alternative commented out underneath
- `IdentityServerHost/Program.cs` — the `IISOptions` configuration
- `IdentityServerHost/IdentityServerHost.csproj` — the `net10.0-windows` target framework
- `IdentityServerHost/Properties/launchSettings.json` — the `IISExpress` profile
- `IdentityServerHost/Clients.cs` — the `mvcsample` client, with the back-channel logout URI
- `Client/Program.cs` — `AcrValues = "idp:Windows"`
