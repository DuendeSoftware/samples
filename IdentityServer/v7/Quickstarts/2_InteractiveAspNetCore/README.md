# Interactive ASP.NET Core sample

This sample shows how to use the OAuth 2.0 **authorization code** grant with an interactive ASP.NET
Core web application — the most common way for a web app to obtain tokens on behalf of a signed-in
user. It also registers a client credentials client, so you can compare the two flows side by side.

### What the sample demonstrates

- a client registered with `AllowedGrantTypes = GrantTypes.Code` and a client secret (a confidential
  client)
- an `AddOpenIdConnect` handler configured for code flow, which turns on PKCE automatically
- a custom identity resource (`verification`) carrying the `email` and `email_verified` claims
- `options.GetClaimsFromUserInfoEndpoint = true` so those claims come from the userinfo endpoint
  rather than being baked into the id token
- storing the tokens in the ASP.NET Core authentication session with `options.SaveTokens = true`
- registering an external identity provider so the login page offers "Sign-in with
  demo.duendesoftware.com"
- an API (`api1`) that is authorized with a policy requiring the `scope` claim

> **Note:** this quickstart does **not** call the API. The `api1` scope is not requested by the `web`
> client, and there is no "Call API" button. Doing that is the subject of
> [`3_AspNetCoreAndApis`](../3_AspNetCoreAndApis).

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Open a browser tab to the `web` application at `https://localhost:5002`. You are redirected
   immediately to the IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`).
1. Approve the consent request. You are returned to the `web` application home page, which lists the
   claims from your session.
1. Click **Signout** in the navigation to see the RP-initiated logout flow, which also runs the
   front-channel sign-out against IdentityServer.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

A `client` credentials client (`client` / `secret`, scope `api1`) is also registered, but nothing in
this sample exercises it.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServer` | `https://localhost:5001` | Login, consent and token issuance |
| `WebClient` | `https://localhost:5002` | The interactive web application (`client` id `web`) |
| `Api` | `https://localhost:6001` | Protected API, present but not called by this quickstart |
| `Client` | _(console)_ | Client credentials demo, independent of the web app |
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

- `src/IdentityServer/Config.cs` — the `web` client and the `verification` identity resource
- `src/IdentityServer/Pages/TestUsers.cs` — the `alice` and `bob` users
- `src/WebClient/Program.cs` — the `AddOpenIdConnect` configuration

To call a protected API from the web app, continue with
[`3_AspNetCoreAndApis`](../3_AspNetCoreAndApis). To get automatic token refresh, see
[`3a_TokenManagement`](../3a_TokenManagement).
