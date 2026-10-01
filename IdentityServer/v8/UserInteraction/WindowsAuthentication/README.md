# Windows authentication sample

This sample shows how to use **Windows Authentication** (Negotiate / Kerberos / NTLM) as the external
identity provider for IdentityServer, so users sign in with their existing Windows account.

### What the sample demonstrates

- configuring `IISOptions` with `AutomaticAuthentication = false`, so the Windows challenge is triggered
  deliberately from the login page rather than automatically on every request
- calling `HttpContext.AuthenticateAsync("Windows")` and inspecting the resulting `WindowsPrincipal`
- using the user's **SID** as the subject identifier: `new IdentityServerUser(wp.FindFirst(ClaimTypes.PrimarySid).Value)`
- translating the user's Windows **group** memberships into `role` claims with
  `WindowsIdentity.Groups.Translate(typeof(NTAccount))`
- challenging the Windows scheme from the login page and re-triggering the same URL on return
- forcing the IdP selection from the client by sending `acr_values=idp:Windows`

> [!IMPORTANT]
> This sample has **hard platform requirements**. It only runs on **Windows**, and the IdentityServer
> host only works when run under **IIS or IIS Express** with the Windows Authentication provider enabled.
> Running it under the Aspire AppHost (which uses `dotnet run` and plain Kestrel) will **not** provide
> Windows authentication.

## Prerequisites

- **Windows** — `IdentityServerHost` targets `net10.0-windows` and uses
  `System.Security.Principal.WindowsIdentity` / `WindowsPrincipal`.
- **IIS Express** (or IIS) with the ASP.NET Core Hosting Bundle installed and the **Windows
  Authentication** provider enabled (Negotiate and/or NTLM). The `IISExpress` launch profile in
  `IdentityServerHost/Properties/launchSettings.json` already sets `iisSettings.windowsAuthentication: true`.
- **Kerberos** additionally requires a domain-joined machine. A standalone or workgroup machine will
  fall back to NTLM.
- A browser that supports integrated Windows authentication — Internet Explorer / Edge (IE mode), or
  another browser that can delegate to Windows.
- .NET 10 SDK and a trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## How to Run

1. Start the IdentityServer host under the **IISExpress** profile, e.g. from Visual Studio, or with
   `dotnet run --project IdentityServerHost` launched from an IIS Express profile. It listens on
   `https://localhost:44324/`.
1. Start the client:

   ```bash
   dotnet run --project WindowsAuthentication.AppHost
   ```

   or simply `dotnet run --project Client` to run the client on its own.

1. Open a browser tab to the client at `https://localhost:44300`.
1. The client's OpenID Connect handler adds `acr_values=idp:Windows` and redirects to
   `https://localhost:44324/account/login`.
1. The login page immediately redirects to `/Account/Login/Windows` — **the local username and password
   form is never shown**. The page then calls `Challenge("Windows")`, which triggers the Windows
   logon prompt or the integrated-authentication handshake.
1. After you authenticate, you land back on the client's `Home/Secure` page. Look at the claims:
   - `sub` is your Windows **SID**
   - `name` is your Windows account name
   - the `role` claims are your **Windows group names** (for example `BUILTIN\Users` or
     `BUILTIN\Administrators`), translated from `WindowsIdentity.Groups`

There are no sample usernames or passwords to type — you authenticate as whichever Windows account you
are logged in to. A `TestUsers.cs` file exists in the project, but it is not registered with
`AddTestUsers` and the local login path is not reachable while the page always redirects to Windows.

## Projects and URLs

| Project | URL | Notes |
|---------|-----|-------|
| `IdentityServerHost` | `https://localhost:44324/` | must be hosted by IIS / IIS Express |
| `Client` | `https://localhost:44300` | **open this one** |
| `WindowsAuthentication.AppHost` (Aspire dashboard) | `https://localhost:17147` | starts the client only, effectively |

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/Pages/Account/Login/Windows.cshtml.cs` — the whole sample: group claims, the SID
  subject, and the Windows challenge
- `IdentityServerHost/Program.cs` — the `IISOptions` configuration
- `IdentityServerHost/IdentityServerHost.csproj` — the `net10.0-windows` target framework
- `IdentityServerHost/Properties/launchSettings.json` — the `IISExpress` profile
- `Client/Program.cs` — `AcrValues = "idp:Windows"`
