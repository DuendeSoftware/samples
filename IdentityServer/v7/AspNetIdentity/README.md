# ASP.NET Core Identity sample

This sample shows how to combine Duende IdentityServer with ASP.NET Core Identity, so that the
default ASP.NET Core Identity UI (login, register, manage account) is the login experience that
IdentityServer uses.

### What the sample demonstrates

- `AddAspNetIdentity<IdentityUser>()` to plug ASP.NET Core Identity into IdentityServer, so
  IdentityServer issues tokens for ASP.NET Core Identity users
- `AddDefaultIdentity<IdentityUser>()` with a SQLite `ApplicationDbContext`, and
  `options.SignIn.RequireConfirmedAccount = true` — a user whose email is not confirmed cannot sign
  in
- using the default ASP.NET Core Identity UI for login, registration and account management, hosted
  in the same application as the token server
- a customized Identity `Logout` page that calls
  `IIdentityServerInteractionService.GetLogoutContextAsync` and exposes `SignOutIframeUrl`, so
  IdentityServer's logout prompt and its sign-out iframe work with the Identity UI
- seeding a local SQLite user store from the command line, and a **Seed Database** command on the
  Aspire dashboard resource that does the same thing
- a public client using `GrantTypes.Implicit` with `FrontChannelLogoutUri`, which signs in through
  the Identity UI and dumps the resulting claims

## How to Run

1. Seed the database. In the Aspire dashboard click **Seed Database** on the `identityserver`
   resource, or run the seed manually:

   ```bash
   dotnet run --project IdentityServerAspNetIdentity -- /seed
   ```

   > **Note:** seeding calls `EnsureDeleted()` before `Migrate()`, so the SQLite database is
   > **recreated from scratch every time**. Any user you register through the Identity UI is lost
   > the next time you seed.

1. Run the Aspire project:

   ```bash
   dotnet run --project AspNetIdentity.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17237`.

1. Open a browser tab to the `client` application at `https://localhost:5002`.
1. Click on the **Secure** tab. You will be redirected to the IdentityServer application hosted at
   `https://localhost:5001`.
1. Log in with the seeded user (see below) on the ASP.NET Core Identity login page.
1. You will be redirected back to the `client` application, which shows the claims from the tokens.
   Alice has `name`, `given_name`, `family_name` and `website`; Bob has those plus a custom
   `location` claim of `somewhere`.
1. Use the **Logout** button in the client navigation. The custom Identity `Logout` page asks
   IdentityServer whether a sign-out prompt is needed and renders the sign-out iframe so the
   session ends at the token server too.

You can also go straight to `https://localhost:5001` to use the Identity UI directly — login,
register, manage account — and IdentityServer's own home, diagnostics and grants pages.

## Seeded users

| Username | Password | Notes |
|----------|----------|-------|
| `AliceSmith@email.example` | `Pass123$` | Email is confirmed, no additional claims |
| `BobSmith@email.example` | `Pass123$` | Adds a custom `location` claim of `somewhere` |

The username and the email address are the same string. Note that `RequireConfirmedAccount` is on,
which both seeded users satisfy because `EmailConfirmed = true`.

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerAspNetIdentity` | `https://localhost:5001` |
| `Client` | `https://localhost:5002` |
| `AspNetIdentity.AppHost` (Aspire dashboard) | `https://localhost:17237` |

The solution file is `AspNetIdentitySample.sln`; it contains all three projects plus the shared
`Aspire.ServiceDefaults`.

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

No database server is needed — the sample uses a local SQLite file (`Users.db`).

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerAspNetIdentity/Program.cs` — the `AddDefaultIdentity<IdentityUser>()`,
  `AddAspNetIdentity<IdentityUser>()` and `/seed` switch calls
- `IdentityServerAspNetIdentity/SeedData.cs` — the test users
- `IdentityServerAspNetIdentity/Areas/Identity/Pages/Account/Logout.cshtml.cs` — the
  IdentityServer-aware logout page
- `IdentityServerAspNetIdentity/Areas/Identity/IdentityHostingStartup.cs` — how the Identity UI area
  is mounted
- `Client/Program.cs` and `Client/Pages/Secure.cshtml` — the OIDC client
- `AspNetIdentity.AppHost/AppHost.cs` — the "Seed Database" command

To add **passkeys** on top of ASP.NET Core Identity, see the sibling
[`AspNetIdentityPasskeys`](../AspNetIdentityPasskeys) sample.
