# JavaScript client without a backend sample

This is the counterpart to [`6_JS_with_backend`](../6_JS_with_backend). Here the browser application
talks to IdentityServer and the API **directly**, using the `oidc-client.js` library. There is no
backend, so the access token and refresh token are held in the browser.

It is the simpler arrangement and the right one for a pure SPA with no server-side secrets to
protect. The trade-off is that a token in `localStorage` or `sessionStorage` is reachable by any
script running on the page, so XSS becomes a token-theft problem.

### What the sample demonstrates

- a **public** client: `RequireClientSecret = false`, registered with `AllowedCorsOrigins =
  { "https://localhost:5003" }` so the browser origin is allowed to call the endpoints
- authorization code flow **with PKCE** driven by `oidc-client.js`, which is what makes a
  secret-less client safe
- a static-file-only ASP.NET Core project — `Program.cs` is just `UseDefaultFiles()`,
  `UseStaticFiles()` and `Run()`. The server has no role beyond serving two HTML files and the
  JavaScript libraries.
- the SPA settings: `authority: "https://localhost:5001"`, `client_id: "js"`,
  `response_type: "code"`, `scope: "openid profile api1"`
- the `callback.html` redirect target that the code flow returns to
- `signinRedirect()` / `signoutRedirect()` and `getUser()` for session handling
- calling `https://localhost:6001/identity` straight from the browser with the access token in the
  `Authorization` header

> **Note:** because the browser calls the API directly, the API's CORS configuration has to allow
> `https://localhost:5003`. That is what `AllowedCorsOrigins` on the client registration is for.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Open a browser tab to `https://localhost:5003`. You see three buttons: **Login**, **Call API** and
   **Logout**.
1. Click **Login**. The browser navigates to the IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the consent request. You are returned
   to `https://localhost:5003/callback.html`, which `oidc-client.js` consumes and then redirects back
   to `index.html`.
1. Open the browser dev tools. Go to **Application → Local Storage** and you can see the tokens the
   browser is holding. This is the concrete difference from the BFF sample.
1. Click **Call API**. The SPA calls `https://localhost:6001/identity` directly with a bearer token
   and renders the returned claims. The request shows up in the network tab as originating from
   `localhost:5003`, not from a server.
1. Click **Logout** to end the session at IdentityServer and clear the local tokens.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServer` | `https://localhost:5001` | Login, consent and token issuance |
| `JavaScriptClient` | `https://localhost:5003` | Static SPA only — no backend logic |
| `WebClient` | `https://localhost:5002` | A separate ASP.NET Core MVC client, not part of this demo |
| `Api` | `https://localhost:6001` | Protected API, `GET /identity` |
| `Client` | _(console)_ | Client credentials demo, independent of the SPA |
| `AppHost` | `https://localhost:17236` | Aspire dashboard |

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

- `src/JavaScriptClient/wwwroot/app.js` — the `Oidc.UserManager` settings and the API call
- `src/JavaScriptClient/wwwroot/callback.html` — the code flow redirect target
- `src/JavaScriptClient/Program.cs` — how little the "backend" does
- `src/IdentityServer/Config.cs` — the public `js` client with `AllowedCorsOrigins`

To move the tokens out of the browser and into a server-side session, see
[`6_JS_with_backend`](../6_JS_with_backend).
