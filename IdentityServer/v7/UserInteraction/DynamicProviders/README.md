# Dynamic Identity Providers sample

IdentityServer can add external identity providers at runtime, without restarting or reconfiguring
the server. This sample keeps those provider definitions in a database, so you can see them appear on
the login page alongside a provider registered in code.

After seeding you get **two** external providers on the login page, which is the point of the sample:
one comes from the database, one is hard-coded.

### What the sample demonstrates

- storing OIDC provider definitions in the configuration database, using the `IdentityProviders`
  table and the `OidcProvider` model — IdentityServer reads them and turns each one into an external
  authentication scheme automatically
- `AddConfigurationStore()` and `AddOperationalStore()` with SQLite, the latter with
  `options.EnableTokenCleanup = true` for automatic token cleanup
- `options.Caching.IdentityProviderCacheDuration = TimeSpan.FromMinutes(15)` together with
  `AddConfigurationStoreCache()`. This is the important operational detail: IdentityServer caches the
  provider list, so **adding a provider to the database does not take effect immediately**. Wait up
  to 15 minutes, or restart the IdentityServer, before expecting a change to show up.
- `options.EmitStaticAudienceClaim = true`
- a second, static provider registered in code with
  `AddOpenIdConnect("oidc", "Sign-in with demo.duendesoftware.com", ...)`, using
  `IdentityServerConstants.ExternalCookieAuthenticationScheme` as the sign-in scheme and
  `IdentityServerConstants.SignoutScheme` as the sign-out scheme
- the `ExternalProviders` / `VisibleExternalProviders` logic in the login page model, which is what
  renders the buttons
- `AddDataProtection().SetApplicationName("IdentityServer")`, required so the external cookie is
  shared across all the providers

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project IdentityServer.AppHost
   ```

   > The AppHost project is called `IdentityServer.AppHost`, not `DynamicProviders.AppHost`.

   The Aspire dashboard opens at `https://localhost:17196` and shows two resources:
   `identityserverhost` and `client`.

1. Click the highlighted **Seed Database** button on the `identityserverhost` resource and wait for
   it to report success. This migrates the SQLite database and populates the clients, resources,
   scopes and identity providers. Nothing works until you do this.
1. Open a browser tab to the `client` application at `https://localhost:44300`. Click **Secure** in
   the navigation; you are redirected to the IdentityServer login page at `https://localhost:5001`.
1. Look at the login page. Below the username and password form there are now **two** external
   provider buttons:
   - **IdentityServer (dynamic)** — seeded into the `IdentityProviders` table
   - **Sign-in with demo.duendesoftware.com** — registered in `Program.cs`
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the request to see the local flow.
1. Sign out, then click **Sign-in with demo.duendesoftware.com**. You are redirected to
   `https://demo.duendesoftware.com`, which needs internet access. Log in there with `alice` /
   `alice`, approve, and you are returned to the `client` with claims from the external provider.
1. To see the dynamic provider work the same way, click **IdentityServer (dynamic)** instead. It
   points at the same demo authority but uses a different client id (`login`) and a different
   display name, so you can tell the two paths apart.
1. If you want to add your own provider, insert a row into the `IdentityProviders` table in the
   SQLite database, then restart the IdentityServer so the cache is cleared.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

Both external providers use the public demo server at `https://demo.duendesoftware.com`, which
accepts `alice` / `alice`.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerHost` | `https://localhost:5001` | Login, consent and token issuance; owns the SQLite database |
| `Client` | `https://localhost:44300` | The web application that signs in |
| `IdentityServer.AppHost` | `https://localhost:17196` | Aspire dashboard, with the **Seed Database** button |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)
- Internet access, if you want to complete an external provider sign-in

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/Program.cs` — the store configuration, the 15 minute cache duration and the
  static provider registration
- `IdentityServerHost/SeedData.cs` — the `OidcProvider` row added to the `IdentityProviders` table
- `IdentityServerHost/Pages/Account/Login/Index.cshtml.cs` — how `VisibleExternalProviders` is built
- `IdentityServer.AppHost/AppHost.cs` — the "Seed Database" command

For the WS-Federation flavour of dynamic providers, see the sibling
[`WsFederationDynamicProviders`](../WsFederationDynamicProviders) sample.
