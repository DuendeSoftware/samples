# WS-Federation dynamic providers sample

This sample shows how to build a **custom dynamic provider type** on top of IdentityServer's dynamic
provider infrastructure. It implements WS-Federation (`Microsoft.AspNetCore.Authentication.WsFederation`)
as a provider that is stored in the configuration database, alongside a regular OIDC dynamic provider.

This is the more advanced companion to the [`DynamicProviders`](../DynamicProviders/README.md) sample,
which only shows the built-in OIDC support.

### What the sample demonstrates

- `idsvrBuilder.AddWsFedDynamicProvider()`, which registers a new provider type with
  `options.DynamicProviders.AddProviderType<WsFederationHandler, WsFederationOptions, WsFedProvider>("wsfed")`
- a `WsFedProvider` record that adds `MetadataAddress`, `RelyingPartyId` and `AllowIdpInitiated` on top of
  the standard `IdentityProvider`
- `EfWsFedProviderStore`, an `IdentityProviderStore` that maps the stored row to the typed provider
  (an `InMemoryWsFedProviderStore` is included as a testing alternative)
- `WsFedConfigureOptions`, which maps the stored values onto `WsFederationOptions` — `Wtrealm` from
  `RelyingPartyId`, `AllowUnsolicitedLogins` from `AllowIdpInitiated`, and a `OnRedirectToIdentityProvider`
  handler that rewrites `Wreply` during sign-out
- registering the options and handler manually with `TryAddEnumerable` / `TryAddTransient`, since the
  static `AddWsFederation` helper is not used
- the login page listing all three kinds of provider side by side: static scheme, dynamic OIDC provider
  and dynamic WS-Federation provider

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project WsFederationDynamicProviders.AppHost
   ```

1. In the Aspire dashboard, click **Seed Database** on the `identityserverhost` resource. The clients,
   resources and both dynamic providers live in SQLite, so nothing works until you do this. You can
   also run it manually with `dotnet run --project IdentityServerHost -- /seed`.
1. Open a browser tab to the client at `https://localhost:44300`. It redirects to the login page at
   `https://localhost:5001/account/login`.
1. The login page shows a **Local Account** card and an **External Account** card with three buttons:
   - **Sign-in with demo.duendesoftware.com** — the statically registered `oidc` scheme
   - **IdentityServer** — the dynamic OIDC provider `demoidsrv` from the database
   - **Local ADFS** — the dynamic WS-Federation provider `adfs` from the database
1. Log in locally as `alice` / `alice`, or click one of the two working external buttons and log in as
   `alice` / `alice` on `https://demo.duendesoftware.com`.
1. Visit `https://localhost:5001/diagnostics` to see the session claims, and `https://localhost:5001/grants`
   to see the stored grants.

> [!IMPORTANT]
> The seeded ADFS provider points at `https://adfs4.local/federationmetadata/2007-06/federationmetadata.xml`,
> which is a **placeholder host that does not exist**. Clicking **Local ADFS** will fail to retrieve the
> metadata. To exercise the WS-Federation path you need to edit the seeded provider in
> `IdentityServerHost/SeedData.cs` and point `MetadataAddress` at a real ADFS server.
>
> The local login and the two `demo.duendesoftware.com` options work as-is, and they are enough to see
> the dynamic provider plumbing.

> **Note:** `Client/Program.cs` unconditionally sets `AcrValues = "idp:adfs"`, which collapses the login
> page to a single-provider view and disables local login. Comment that line out if you want to see the
> full provider list described above.

## Seeded users

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
| `WsFederationDynamicProviders.AppHost` (Aspire dashboard) | `https://localhost:17055` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- Internet access to `https://demo.duendesoftware.com` for the external providers. Local login works
  without it.
- A real WS-Federation / ADFS server if you want to exercise the WS-Federation path (see above).

The sample itself is cross-platform; only the ADFS server it talks to is a Windows product.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/WsFed/IdentityServerBuilderWsFedExtensions.cs` — `AddWsFedDynamicProvider()` and `AddInMemoryWsFedProviders()`
- `IdentityServerHost/WsFed/WsFedProvider.cs` — the typed provider record
- `IdentityServerHost/WsFed/EfWsFedProviderStore.cs` — mapping the stored row to the typed provider
- `IdentityServerHost/WsFed/WsFedConfigureOptions.cs` — mapping the provider onto `WsFederationOptions`
- `IdentityServerHost/SeedData.cs` — the `adfs` and `demoidsrv` provider rows
- `IdentityServerHost/Pages/Account/Login/Index.cshtml.cs` — enumerating the dynamic schemes
