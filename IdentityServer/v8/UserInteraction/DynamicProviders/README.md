# Dynamic identity providers sample

This sample shows how to add **dynamic external identity providers** to IdentityServer. Instead of being
registered in code, the providers live as rows in the configuration store, so you can add or change an
external IdP without recompiling or redeploying.

### What the sample demonstrates

- an OIDC provider stored in the configuration database (`SeedData.cs` creates an `OidcProvider` with
  scheme `demoidsrv` pointing at `https://demo.duendesoftware.com`)
- a **static** OIDC scheme registered in code alongside it, so you can compare the two side by side on the
  same login page
- `AddConfigurationStoreCache()` with `Caching.IdentityProviderCacheDuration`, which is what makes
  dynamic providers (and the rest of the configuration data) cached rather than re-read on every request
- discovering providers on the login page by combining two sources: the static schemes from
  `IAuthenticationSchemeProvider.GetAllSchemesAsync()` and the dynamic schemes from
  `IIdentityProviderStore.GetAllSchemeNamesAsync()`
- honouring `client.EnableLocalLogin` and `client.IdentityProviderRestrictions` when building the login page
- auto-provisioning external users through the external login callback

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project IdentityServer.AppHost
   ```

   > The AppHost project is named `IdentityServer.AppHost`, not `DynamicProviders.AppHost`.

1. In the Aspire dashboard, click **Seed Database** on the `identityserverhost` resource. The clients,
   resources and the dynamic provider all live in SQLite, so nothing works until you do this. You can
   also run it manually with `dotnet run --project IdentityServerHost -- /seed`.
1. Open a browser tab to the client at `https://localhost:44300`. It redirects to the login page at
   `https://localhost:5001/account/login`.
1. The login page shows a **Local Account** card and an **External Account** card with two buttons:
   - **Sign-in with demo.duendesoftware.com** — the statically registered `oidc` scheme
   - **IdentityServer (dynamic)** — the `demoidsrv` provider loaded from the configuration store
1. Log in locally as `alice` / `alice` to see the local flow.
1. Click either external button to be redirected to `https://demo.duendesoftware.com`, log in as
   `alice` / `alice`, and you will be returned to the client with the external user auto-provisioned.
1. Visit `https://localhost:5001/diagnostics` to see the session claims, and `https://localhost:5001/grants`
   to see the grants that have been stored.

## Seeded users

Local users (in-memory test users):

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

The same credentials also work on the external `https://demo.duendesoftware.com` provider.

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerHost` | `https://localhost:5001` |
| `Client` | `https://localhost:44300` — **open this one** |
| `IdentityServer.AppHost` (Aspire dashboard) | `https://localhost:17197` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- Internet access to `https://demo.duendesoftware.com` for the external providers. Local login works
  without it.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/Pages/Account/Login/Index.cshtml.cs` — merging static and dynamic schemes
- `IdentityServerHost/SeedData.cs` — the `OidcProvider` row that creates the dynamic provider
- `IdentityServerHost/Program.cs` — `AddConfigurationStoreCache()` and the identity provider cache duration
- `IdentityServerHost/Pages/ExternalLogin/Challenge.cshtml.cs` and `Callback.cshtml.cs` — the external flow
- `IdentityServer.AppHost/AppHost.cs` — the **Seed Database** command

For a WS-Federation version of dynamic providers, see
[`WsFederationDynamicProviders`](../WsFederationDynamicProviders/README.md).
