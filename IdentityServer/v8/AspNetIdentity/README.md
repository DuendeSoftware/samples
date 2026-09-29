# ASP.NET Core Identity sample

This sample shows how to combine Duende IdentityServer with ASP.NET Core Identity, so that the
default ASP.NET Core Identity UI (login, register, manage account) is the login experience that
IdentityServer uses.

### What the sample demonstrates

- `AddAspNetIdentity<IdentityUser>()` to plug ASP.NET Core Identity into IdentityServer, so IdentityServer
  issues tokens for ASP.NET Core Identity users
- using the default ASP.NET Core Identity UI for login, registration and account management
- a customized Identity `Logout` page that calls `IIdentityServerInteractionService.GetLogoutContextAsync`
  so IdentityServer's logout prompt and sign-out iframe work with the Identity UI
- seeding a local SQLite user store from the command line
- an interactive OIDC client that signs in through the Identity UI and dumps the resulting claims

## How to Run

1. Seed the database. In the Aspire dashboard click **Seed Database** on the `identityserver`
   resource, or run the seed manually:

   ```bash
   dotnet run --project IdentityServerAspNetIdentity -- /seed
   ```

   > **Note:** seeding calls `EnsureDeleted()` before `Migrate()`, so the SQLite database is
   > **recreated from scratch every time**. Any registered users are lost.

1. Run the Aspire project:

   ```bash
   dotnet run --project AspNetIdentity.AppHost
   ```

1. Open a browser tab to the `client` application at `https://localhost:5002`.
1. Click on the **Secure** tab. You will be redirected to the IdentityServer application hosted at
   `https://localhost:5001`.
1. Log in with the seeded user (see below).
1. You will be redirected back to the `client` application, which shows the claims from the tokens.

You can also go straight to `https://localhost:5001` to use the Identity UI directly (login, register,
manage account).

## Seeded users

| Username | Password | Notes |
|----------|----------|-------|
| `AliceSmith@email.example` | `Pass123$` | Email is confirmed, no additional claims |
| `BobSmith@email.example`  | `Pass123$` | Adds a custom `location` claim |

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerAspNetIdentity` | `https://localhost:5001` |
| `Client` | `https://localhost:5002` |
| `AspNetIdentity.AppHost` (Aspire dashboard) | `https://localhost:17236` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database server is needed — the sample uses a local SQLite file (`Users.db`).

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerAspNetIdentity/Program.cs` — the `AddAspNetIdentity<IdentityUser>()` call and the `/seed` switch
- `IdentityServerAspNetIdentity/SeedData.cs` — the test users
- `IdentityServerAspNetIdentity/Areas/Identity/Pages/Account/Logout.cshtml.cs` — the IdentityServer-aware logout page
- `Client/Program.cs` and `Client/Pages/Secure.cshtml` — the OIDC client
