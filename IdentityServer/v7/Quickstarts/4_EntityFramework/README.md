# Entity Framework Core storage sample

Up to now the quickstarts have kept clients, resources and grants in memory, which means they are
recreated every time the process restarts. This sample moves both the **configuration** data
(clients, identity resources, API scopes) and the **operational** data (authorization codes, refresh
tokens, persisted grants, consent) into SQLite using Entity Framework Core.

### What the sample demonstrates

- `AddConfigurationStore()` — clients, resources and CORS settings read from the database
- `AddOperationalStore()` — codes, tokens, consents and device flow data read from the database
- pointing both stores at a single SQLite file via `options.ConfigureDbContext` and
  `b.UseSqlite(connectionString, sql => sql.MigrationsAssembly(...))`, with the connection string
  hard-coded as `Data Source=Duende.IdentityServer.Quickstart.EntityFramework.db`
- seeding with the `Duende.IdentityServer.EntityFramework` mappers (`client.ToEntity()`) and
  `Duende.IdentityServer.EntityFramework.DbContexts` (`ConfigurationDbContext`,
  `PersistedGrantDbContext`)
- a separate `Data/Migrations` directory so the store migrations are not confused with your own EF
  migrations
- `Database.Migrate()` plus idempotent seeding at startup, so the database is created and populated
  automatically — there is no manual step
- Serilog request logging, so you can watch token endpoint traffic
- an *optional* Google external provider, enabled only when `Authentication:Google:ClientId` and
  `Authentication:Google:ClientSecret` are present in configuration (they are not set by default)

> **Note:** seeding is guarded by `if (!context.Clients.Any())` and so on. If you change `Config.cs`
> after the first run, the database already has rows and your changes are ignored. Delete
> `src/IdentityServer/Duende.IdentityServer.Quickstart.EntityFramework.db` and restart to pick them up.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Open a browser tab to the `web` application at `https://localhost:5002`. You are redirected to the
   IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the consent request.
1. Click **Call API** to make an authorized call to `https://localhost:6001/identity`.
1. Stop the `identityserver` resource, then start it again. You stay logged in — the session and the
   refresh token survived the restart, because they are now in the database rather than in memory.
   That is the main thing this quickstart is demonstrating.
1. Look at `src/IdentityServer/Duende.IdentityServer.Quickstart.EntityFramework.db` (created on first
   run, and git-ignored) with any SQLite browser to see the rows.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

Test users are registered with `AddTestUsers`, which remains in-memory — only the IdentityServer
configuration and operational stores moved to the database.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServer` | `https://localhost:5001` | Login, consent and token issuance; owns the SQLite database |
| `WebClient` | `https://localhost:5002` | The interactive web application |
| `Api` | `https://localhost:6001` | Protected API, `GET /identity` |
| `Client` | _(console)_ | Client credentials demo, independent of the web app |
| `AppHost` | `https://localhost:17236` | Aspire dashboard |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `src/IdentityServer/HostingExtensions.cs` — the store configuration and `InitializeDatabase`
- `src/IdentityServer/Data/Migrations` — the store migrations
- `src/IdentityServer/Config.cs` — the configuration that gets seeded

To use ASP.NET Core Identity for users instead of test users, see
[`5_AspNetIdentity`](../5_AspNetIdentity).
