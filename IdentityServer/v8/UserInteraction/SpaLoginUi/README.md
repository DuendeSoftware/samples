# SPA login UI sample

This sample shows how to replace IdentityServer's default Razor Pages user interaction pages with your
**own** pages — here, hand-written static HTML plus a small JSON API. The result is a login experience that
looks and behaves like a single-page application.

### What the sample demonstrates

- pointing IdentityServer's interaction URLs at static files instead of Razor Pages:
  `LoginUrl = "/login.html"`, `ConsentUrl = "/consent.html"`, `LogoutUrl = "/logout.html"`,
  `ErrorUrl = "/error.html"`
- a controller that implements each interaction with `IIdentityServerInteractionService`:
  - `GetAuthorizationContextAsync` — who is asking for what, used to render the login and consent pages
  - `GrantConsentAsync` / `DenyAuthorizationAsync` — recording the user's consent decision
  - `GetErrorContextAsync` — rendering the error page
  - `GetLogoutContextAsync` — including the sign-out iframe URL for front-channel logout
- signing the user in and out with `HttpContext.SignInAsync` / `SignOutAsync` against a `TestUserStore`
- serving the static pages with `app.UseDefaultFiles()` and `app.UseStaticFiles()`
- a `RequireConsent = true` client, so the consent page is always exercised

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project SpaLoginUi.AppHost
   ```

1. In the Aspire dashboard, click the endpoint link for the `client` resource to open
   `https://localhost:44300`, or navigate there directly.
1. You are redirected to `https://localhost:5001/login.html?returnUrl=...` — a plain static HTML page,
   not a Razor page.
1. Log in as `alice` / `alice` and click **Login**. The page posts JSON to `/spa/login` and then
   navigates to the returned `validReturnUrl`.
1. You are then shown `https://localhost:5001/consent.html?returnUrl=...`. The page calls
   `GET /spa/context` and renders the requesting client and the requested scopes. Optionally tick
   **Remember My Decision** and click **Yes, Allow**. Clicking **No, Do Not Allow** instead takes you back
   to the client with an access denied error.
1. You land back on the client application showing your claims.
1. Click **Logout** in the client navigation. You are redirected to
   `https://localhost:5001/logout.html?logoutId=...`. If the client requests a logout prompt you will be
   asked to confirm; otherwise the page loads the sign-out iframe in a hidden `<iframe>` and shows a link
   to return to the application.

## Seeded users

| Username | Password | Subject id |
|----------|----------|------------|
| `alice` | `alice` | `818727` |
| `bob` | `bob` | `88421113` |

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerHost` | `https://localhost:5001` |
| `Client` | `https://localhost:44300` — **open this one** |
| `SpaLoginUi.AppHost` (Aspire dashboard) | `https://localhost:17047` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database seeding, external identity provider or internet access is needed.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/SpaEndpoints.cs` — the whole JSON API
- `IdentityServerHost/Program.cs` — the four `options.UserInteraction.*Url` overrides
- `IdentityServerHost/wwwroot/login.html`, `consent.html`, `logout.html`, `error.html` — the static pages
- `IdentityServerHost/Config.cs` — the `interactive` client with `RequireConsent = true`
