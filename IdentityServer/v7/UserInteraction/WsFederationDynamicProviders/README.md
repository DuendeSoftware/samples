# WS-Federation Dynamic Providers sample

[WS-Federation](https://learn.microsoft.com/openspecs/windows_protocols/ms-wsfdr/1fc0b6b0-1a1d-45a5-8b3b-83d1e0a4e0a8)
is the protocol Active Directory Federation Services (ADFS) and other corporate federation services
speak. It predates OpenID Connect and is still the right choice when the identity provider is
Microsoft Entra ID in a Windows-only corporate network.

This sample registers WS-Federation identity providers dynamically, from a database, so you can see
how they surface on the login page.

### What the sample demonstrates

- `idsvrBuilder.AddWsFedDynamicProvider()` — the Duende API that turns rows in the
  `IdentityProviders` table into WS-Federation external authentication schemes
- `AddIdentityProviderStore<EfWsFedProviderStore>()` — the storage abstraction that lets the
  providers live in the configuration database rather than in memory
- an in-memory alternative, `AddInMemoryWsFedProviders(...)`, present but commented out in
  `Program.cs` alongside the `InMemoryWsFedProviderStore.cs` file in the same folder, so you can
  compare the two
- storing a WS-Federation provider as a `WsFedProvider` row in the configuration database, with a
  `Scheme`, `MetadataAddress` (the federation metadata URL), `RelyingPartyId` (the `wtrealm`) and a
  `DisplayName` shown on the login page button
- storing an OIDC provider as an `OidcProvider` row in the **same** table, so both protocols can be
  configured dynamically side by side
- `WsFedConfigureOptions.cs`, which maps the `WsFedProvider` fields onto
  `WsFederationOptions.MetadataAddress` and `WsFederationOptions.Wtrealm`
- `AddConfigurationStore()` and `AddOperationalStore()` with SQLite, plus
  `options.EnableTokenCleanup = true`
- `options.EmitStaticAudienceClaim = true`
- `AddOpenIdConnect("oidc", "Sign-in with demo.duendesoftware.com", ...)` as a *second*, OIDC-based
  provider, so the login page shows both protocols side by side
- `AddDataProtection().SetApplicationName("IdentityServer")`

> **Important:** the seed registers a WS-Federation provider called **Local ADFS** pointing at
> `https://adfs4.local/federationmetadata/2007-06/federationmetadata.xml`. That host does not exist
> — it is a placeholder so the configuration is illustrative. Clicking that button will fail to
> resolve. The **IdentityServer** provider that sits alongside it is an *OIDC* provider, not
> WS-Federation, and it does work — but the sample `client` is hard-wired to select the ADFS one, so
> see the note under How to Run.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project WsFederationDynamicProviders.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17050` and shows two resources:
   `identityserverhost` and `client`.

1. Click the highlighted **Seed Database** button on the `identityserverhost` resource and wait for
   it to report success. This migrates the SQLite database and populates the clients, resources,
   scopes and the two WS-Federation providers. Nothing works until you do this.

   > Seeding calls `Migrate()` only and guards each step with `if (!context.Clients.Any())`-style
   > checks, so it is safe to run repeatedly. Change `Config.cs` and you will need to delete the
   > SQLite file to pick the changes up.

1. Open a browser tab to the `client` application at `https://localhost:44300`. Click **Secure** in
   the navigation; you are redirected to the IdentityServer login page at `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the request.
1. Sign out and look at the login page. You now see three external provider buttons:
   - **IdentityServer** — a dynamic **OIDC** provider seeded into the `IdentityProviders` table,
     pointing at `https://demo.duendesoftware.com` as client `login`
   - **Local ADFS** — a dynamic **WS-Federation** provider, the `adfs4.local` placeholder
   - **Sign-in with demo.duendesoftware.com** — a static OIDC provider registered in `Program.cs`
1. **Local ADFS** will not work: `adfs4.local` is a placeholder hostname. This is expected, and it
   is the only WS-Federation provider in the sample.
1. To complete a working external sign-in, click **IdentityServer** on the login page directly. You
   are redirected to `https://demo.duendesoftware.com`, log in with `alice` / `alice`, approve, and
   are returned.

> **Note:** the sample `client` hard-codes `ctx.ProtocolMessage.AcrValues = "idp:adfs"` in
> `Client/Program.cs`, which tells IdentityServer to auto-select the ADFS provider on every request.
> That is why clicking **Secure** in the client sends you straight at the broken placeholder. The
> line is there to show how a client can request a specific identity provider by name; change it to
> `"idp:demoidsrv"` and restart the client to exercise the working one.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

The seeded **IdentityServer** OIDC provider relies on the public demo server at
`https://demo.duendesoftware.com`, which accepts `alice` / `alice`. The **Local ADFS** provider has no
reachable server at all.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerHost` | `https://localhost:5001` | Login, consent and token issuance; owns the SQLite database |
| `Client` | `https://localhost:44300` | The web application that signs in |
| `WsFederationDynamicProviders.AppHost` | `https://localhost:17050` | Aspire dashboard, with the **Seed Database** button |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)
- Internet access, and a reachable ADFS or federation endpoint, to actually complete a
  WS-Federation sign-in

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/Program.cs` — `AddWsFedDynamicProvider`, `EfWsFedProviderStore`, and the
  commented-out in-memory alternative
- `IdentityServerHost/SeedData.cs` — the `OidcProvider` and `WsFedProvider` rows
- `IdentityServerHost/WsFedConfigureOptions.cs` — the `WsFederationOptions` mapping
- `IdentityServerHost/WsFedProvider.cs` — the provider model
- `IdentityServerHost/InMemoryWsFedProviderStore.cs` — the alternative store
- `Client/Program.cs` — the `AcrValues = "idp:adfs"` provider selection
- `WsFederationDynamicProviders.AppHost/AppHost.cs` — the "Seed Database" command

For OIDC-based dynamic providers, see the sibling
[`DynamicProviders`](../DynamicProviders) sample.
