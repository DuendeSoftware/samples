# ASP.NET Core and APIs sample

This sample extends [`2_InteractiveAspNetCore`](../2_InteractiveAspNetCore) by adding a protected
web API and calling it from the signed-in web application using the access token that the
authorization code flow produced.

### What the sample demonstrates

- the same code flow client as quickstart 2, now requesting the `api1` API scope **in addition** to
  `openid`, `profile` and the custom `verification` resource
- `options.SaveTokens = true`, so the access token is retained in the authentication session
- manually reading the access token out of the session and attaching it as a bearer token to an
  outgoing `HttpClient` request
- a protected API that validates the token with `AddJwtBearer` against the IdentityServer's
  authority, and authorizes with a policy requiring the `scope` claim to contain `api1`
- `options.TokenValidationParameters.ValidateAudience = false`, because the quickstart uses an API
  scope rather than an API resource, so there is no audience to validate

> **Note:** this quickstart reads the token by hand. There is no automatic caching or refreshing.
> [`3a_TokenManagement`](../3a_TokenManagement) shows how Duende AccessTokenManagement does this for
> you.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Open a browser tab to the `web` application at `https://localhost:5002`. You are redirected to the
   IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the consent request. Note that this
   time the consent screen also lists **My API** (`api1`).
1. You are returned to the `web` application. Click **Call API**.
1. The web app attaches the access token to a `GET https://localhost:6001/identity` request, and the
   API echoes back the claims from the token — including the `client_id` and `scope` claims that
   IdentityServer added.
1. Try it again after signing out and back in, or change the API to reject the request to see how the
   failure surfaces.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServer` | `https://localhost:5001` | Login, consent and token issuance |
| `WebClient` | `https://localhost:5002` | The interactive web application |
| `Api` | `https://localhost:6001` | Protected API, `GET /identity` |
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

- `src/IdentityServer/Config.cs` — the `api1` scope and the `web` client
- `src/WebClient/Program.cs` — `SaveTokens` and the `api1` scope request
- `src/WebClient/Pages/Index.cshtml.cs` — reading the token out of the session
- `src/Api/Program.cs` — the `ApiScope` authorization policy

For automatic token caching and refresh, continue with
[`3a_TokenManagement`](../3a_TokenManagement). For persistent storage, see
[`4_EntityFramework`](../4_EntityFramework).
