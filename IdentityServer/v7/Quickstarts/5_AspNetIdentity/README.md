# ASP.NET Core Identity sample

The previous quickstarts used `AddTestUsers`, which is an in-memory user store intended for demos.
This sample replaces it with **ASP.NET Core Identity** backed by SQLite, which is what you would
actually use in a real application — you get password hashing, account lockout, email confirmation
and roles for free.

### What the sample demonstrates

- `AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>()`
  with a SQLite `ApplicationDbContext`
- `AddAspNetIdentity<ApplicationUser>()`, which bridges ASP.NET Core Identity into IdentityServer so
  the login page and user management work against the Identity user store
- a custom `ApplicationUser` with an extra `FavoriteColor` property
- `AddProfileService<CustomProfileService>()`, deriving from `AspNetIdentityProfileService` to add a
  `favorite_color` claim when the user has one
- a `color` identity resource (`new IdentityResource("color", new[] { "favorite_color" })`) that
  makes that claim requestable, and which the `web` client is allowed to ask for
- a "Seed Database" command on the Aspire dashboard resource that runs the IdentityServer with
  `/seed` to migrate and populate the user database

> **Note:** seeding calls `EnsureDeleted()` before `Migrate()`, so **every seed run drops the
> database** and recreates the users. Any user you register yourself is lost. This is deliberate for
> a sample — it guarantees you always start from the documented accounts.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236`.

1. Click the highlighted **Seed Database** button on the `identityserver` resource. Wait for it to
   report success. You must do this **before** signing in, or there will be no users and the login
   page will reject every attempt.
1. Open a browser tab to the `web` application at `https://localhost:5002`. You are redirected to the
   IdentityServer at `https://localhost:5001`.
1. Log in with `alice` / `Pass123$` (or `bob` / `Pass123$`) and approve the consent request. The
   consent screen lists **My API** (`api1`) and the custom **color** resource.
1. You are returned to the `web` application. The home page lists the claims, and you should see
   `favorite_color` with the value `red`, alongside `email` and `email_verified`.
1. Click **Signout**, then sign in as `bob` to see the claim **disappear** — Bob is seeded without a
   `FavoriteColor`, and `CustomProfileService` only adds the claim when the value is non-empty.
1. Sign in once more and watch the `AspIdUsers.db` file in
   `src/IdentityServerAspNetIdentity/` to see the hashed passwords in the `AspNetUsers` table.

## Credentials

| Username | Email | Password | `favorite_color` |
|----------|-------|----------|------------------|
| `alice` | `AliceSmith@email.example` | `Pass123$` | `red` |
| `bob` | `BobSmith@email.example` | `Pass123$` | _(none)_ |

The **username** is what you type on the login page, not the email address.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerAspNetIdentity` | `https://localhost:5001` | Login, consent and token issuance; owns `AspIdUsers.db` |
| `WebClient` | `https://localhost:5002` | The interactive web application |
| `Api` | `https://localhost:6001` | Protected API, registered but not called by this quickstart |
| `Client` | _(console)_ | Client credentials demo, independent of the web app |
| `AppHost` | `https://localhost:17236` | Aspire dashboard, with the **Seed Database** button |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `src/IdentityServerAspNetIdentity/HostingExtensions.cs` — `AddAspNetIdentity` and the profile service
- `src/IdentityServerAspNetIdentity/CustomProfileService.cs` — where `favorite_color` is emitted
- `src/IdentityServerAspNetIdentity/Config.cs` — the `color` identity resource
- `src/IdentityServerAspNetIdentity/SeedData.cs` — the user seeding
- `src/AppHost/AppHost.cs` — the "Seed Database" command

For a much richer user management sample — registration, password reset, MFA and account lockout —
see the `UserManagement` folder. There is no v7 equivalent of it; the closest v7 material is
[`Basics/MvcBasic`](../../Basics/MvcBasic), which shows the login and logout pages in full.
