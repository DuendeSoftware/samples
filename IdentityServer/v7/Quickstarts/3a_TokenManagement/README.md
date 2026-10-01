# Access Token Management sample

This sample shows how to use [Duende AccessTokenManagement](https://docs.duendesoftware.com/identityserver/access-token-management/)
so the web application never has to juggle access tokens itself. Instead of reading a token out of
the session and attaching it by hand, you declare a named `HttpClient` and the library acquires,
caches, refreshes and attaches the token for you.

It builds on [`3_AspNetCoreAndApis`](../3_AspNetCoreAndApis), which does the same call manually.

### What the sample demonstrates

- requesting `offline_access` so the authorization code flow returns a refresh token
- `AllowOfflineAccess = true` on the `web` client
- `AddOpenIdConnectAccessTokenManagement()` to enable the token cache and the refresh logic
- `AddUserAccessTokenHttpClient("apiClient", ...)` — a named `HttpClient` that transparently carries
  the user's access token
- `GetUserAccessTokenAsync()` for the cases where you want the token itself rather than a client

> **Note:** the `web` client here does not override `AccessTokenLifetime`, so access tokens last for the
> Duende default of one hour. You will therefore usually not observe a refresh while clicking around.
> To watch the library actually refresh, sign in, wait more than an hour, then call the API again —
> the refresh happens transparently and the API keeps returning 200. (The
> [`MvcTokenManagement` Basics sample](../../Basics/MvcTokenManagement/README.md) registers a second
> client with a deliberately short lifetime if you want to see this quickly.)

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Open a browser tab to the `web` application at `https://localhost:5002`. You are redirected to the
   IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the consent request. The consent
   screen also lists **Offline Access** alongside **My API**.
1. You are returned to the `web` application. The home page lists your claims and the properties of
   your authentication session.
1. Click **Call API**. The page creates the named `apiClient` and calls
   `GET https://localhost:6001/identity`; the access token is attached for you.
1. Compare this with quickstart 3: there the page did
   `await HttpContext.GetTokenAsync("access_token")` and set the `Authorization` header itself. The
   equivalent code is still present, commented out, in
   `src/WebClient/Pages/CallApi.cshtml.cs` — swap the two to compare.

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

- `src/WebClient/Program.cs` — `AddOpenIdConnectAccessTokenManagement` and the named `HttpClient`
- `src/WebClient/Pages/CallApi.cshtml.cs` — the managed call, next to the commented-out manual one
- `src/IdentityServer/Config.cs` — `AllowOfflineAccess` on the `web` client

For persistent storage of clients, resources and grants, see
[`4_EntityFramework`](../4_EntityFramework).
