# JavaScript client with a BFF sample

This sample shows a plain JavaScript single-page application that is protected by a
[Backend-for-Frontend (BFF)](https://docs.duendesoftware.com/identityserver/bff). The browser never
sees an access token: the BFF holds the tokens in a server-side session and proxies API calls, adding
the `X-CSRF` header the API endpoints require.

> **Note:** the BFF in this sample *is* the `JavaScriptClient` project. It serves the static SPA from
> `wwwroot` **and** acts as the backend, which is why it listens on `5003` rather than the `5002` used
> by the plain `WebClient`.

### What the sample demonstrates

- `AddBff()` from `Duende.BFF` to set up the BFF services
- `ConfigureOpenIdConnect(...)` with the `bff` client, requesting `api1` and `offline_access`
- `ConfigureCookies(options => options.Cookie.SameSite = SameSiteMode.Strict)` and the
  `X-CSRF: 1` convention that protects against CSRF
- `app.UseBff()` — must sit between `UseAuthentication()` and `UseAuthorization()`
- `app.MapBffManagementEndpoints()`, exposing `/bff/login`, `/bff/logout` and `/bff/user`
- `app.MapRemoteBffApiEndpoint("/remote", ...).WithAccessToken(RequiredTokenType.User)` — a reverse
  proxy that forwards `/remote/*` to the API and attaches the user's access token
- `app.MapGet("/local/identity", ...).AsBffApiEndpoint()` — a local endpoint in the BFF itself,
  protected the same way
- `app.MapBffManagementEndpoints()`'s `bff:logout_url` claim, which the SPA reads to build its
  logout link
- a public client registered with `RequireClientSecret = false` and `AllowedCorsOrigins`

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Open a browser tab to `https://localhost:5003`. You see four buttons: **Login**, **Call Local
   API**, **Call Remote API** and **Logout**.
1. Click **Login**. The SPA navigates to `/bff/login`, which redirects to the IdentityServer at
   `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the consent request. You are returned
   to the SPA.
1. Open the browser's network tab and reload. The SPA calls `GET /bff/user` and renders the result.
   That response contains the user's claims — but **no access token**. This is the whole point of the
   BFF pattern.
1. Click **Call Local API**. The SPA calls `GET /local/identity` with the `X-CSRF: 1` header; the
   BFF answers with a local JSON payload naming you.
1. Click **Call Remote API**. The SPA calls `GET /remote/identity`, which the BFF reverse-proxies to
   `https://localhost:6001/identity`, attaching the access token server-side. The API echoes the
   token claims back.
1. Click **Logout**. The SPA reads the `bff:logout_url` claim and navigates to it, so the session ends
   at IdentityServer as well as in the BFF.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServer` | `https://localhost:5001` | Login, consent and token issuance |
| `JavaScriptClient` | `https://localhost:5003` | The BFF **and** the static SPA |
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

- `src/JavaScriptClient/Program.cs` — the entire BFF setup
- `src/JavaScriptClient/wwwroot/app.js` — the four button handlers and the `X-CSRF` header
- `src/IdentityServer/Config.cs` — the `bff` client registration

To see the same SPA holding tokens in the browser instead, see
[`6_JS_without_backend`](../6_JS_without_backend). For Blazor WebAssembly with a BFF, see
[`7_Blazor`](../7_Blazor).
