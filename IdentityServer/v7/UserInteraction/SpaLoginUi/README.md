# SPA Login UI sample

The default IdentityServer login and consent pages are server-rendered Razor Pages. That works fine
when your application owns the UI, but it means the user interface is coupled to the identity
provider: to change the look of the login screen you have to change the identity provider.

This sample replaces those pages with **static HTML files** that talk to a small set of JSON
endpoints. You can now host the login UI on a CDN, in a design system package, or behind a completely
separate deployment, without touching the token server.

### What the sample demonstrates

- overriding all four user interaction URLs to point at static files:
  - `options.UserInteraction.LoginUrl = "/login.html"`
  - `options.UserInteraction.ConsentUrl = "/consent.html"`
  - `options.UserInteraction.LogoutUrl = "/logout.html"`
  - `options.UserInteraction.ErrorUrl = "/error.html"`
- `SpaEndpoints.cs` — a small controller exposing the same operations the Razor Pages do, as JSON:
  - `GET /spa/context` — wraps `IInteractionService.GetAuthorizationContextAsync`, returning the
    login hint, requested `idp` and `tenant`, the raw scope values, and the client id
  - `POST /spa/login` — validates credentials against the test users and issues a sign-in
  - `POST /spa/consent` — records the consent response
  - `GET /spa/error` — wraps the error interaction
  - `GET`/`POST /spa/logout` — wraps the logout interaction
- the four static pages in `wwwroot` (`login.html`, `consent.html`, `logout.html`, `error.html`),
  each a plain HTML file with inline `fetch` calls to `https://localhost:5001/spa/...`
- a client registered as `interactive` with `GrantTypes.Code`, redirecting to
  `https://localhost:44300/signin-oidc`
- `options.EmitStaticAudienceClaim = true`

> **Known quirk:** `SpaEndpoints` is decorated with `[EnableCors("spa")]`, but the sample never calls
> `AddCors()` to register a policy with that name. The CORS middleware only evaluates the policy when
> the request carries an `Origin` header, so the same-origin browser flow below works fine. If you
> serve these pages from a **different** origin — which is the point of a decoupled UI — you must add
> a matching `AddCors` policy yourself or those requests will throw *"The CORS policy named 'spa' was
> not found."*

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project SpaLoginUi.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17047` and shows two resources:
   `identityserverhost` and `client`.

1. Open a browser tab to the `client` application at `https://localhost:44300`. Click **Secure** in
   the navigation.
1. You are redirected to `https://localhost:5001/login.html` — a static HTML file, not a Razor Page.
   View the page source in the browser: it is plain HTML with an inline script.
1. Log in with `alice` / `alice` (or `bob` / `bob`).
1. The page's `fetch` POSTs your credentials to `https://localhost:5001/spa/login`. Open the browser
   network tab to watch that call. The endpoint validates the credentials, signs you in, and returns
   a redirect URL which the page then navigates to.
1. If the client requests scopes you have not consented to, you land on
   `https://localhost:5001/consent.html`, which calls `GET /spa/context` to load the client name and
   scopes, then `POST /spa/consent` to record your answer.
1. You are returned to the client. Sign out to see `logout.html` and the `/spa/logout` calls, which
   include a **Sign me out automatically** checkbox that passes a `logoutId` through to
   `IIdentityServerInteractionService`.
1. To see the error page, request a scope the client is not allowed. You land on
   `https://localhost:5001/error.html`, which calls `GET /spa/error` and renders the message.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerHost` | `https://localhost:5001` | Token service, plus the `/spa/*` JSON endpoints and static UI |
| `Client` | `https://localhost:44300` | The web application that signs in |
| `SpaLoginUi.AppHost` | `https://localhost:17047` | Aspire dashboard |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

No database or seeding step is required — the configuration is in-memory and the test users are
registered with `AddTestUsers`.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/Program.cs` — the four `UserInteraction.*Url` overrides
- `IdentityServerHost/SpaEndpoints.cs` — the JSON API behind the pages
- `IdentityServerHost/wwwroot/login.html` — a static login page calling `POST /spa/login`
- `IdentityServerHost/wwwroot/consent.html` — the `GET /spa/context` and `POST /spa/consent` pair
- `IdentityServerHost/Config.cs` — the `interactive` client registration
