# ASP.NET Core Identity with Passkeys sample

[Passkeys](https://passkeys.dev/) are a passwordless sign-in method built on WebAuthn: the browser
stores a cryptographic key pair on the device and the server stores only the public half. To sign
in, the device proves possession of the private key — there is no shared secret to phish, reuse or
leak.

This sample adds passkey registration and sign-in to a Duende IdentityServer that uses ASP.NET Core
Identity for its user store.

### What the sample demonstrates

- `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3` on `AddIdentity<ApplicationUser,
  IdentityRole>`, which is the Identity schema version that introduces the built-in passkey store
  (it creates an `AspNetUserPasskeys` table)
- `app.MapPasskeyEndpoints()`, which maps two endpoints:
  - `POST /Account/PasskeyCreationOptions`
  - `POST /Account/PasskeyRequestOptions`
- `builder.Services.Configure<IdentityPasskeyOptions>(...)` with a `ValidateOrigin` predicate,
  restricted in Development to `https://localhost:5001`. WebAuthn requires the origin to be checked
  explicitly, and this is where you would allow your real production origins
- a custom `<passkey-submit>` **TagHelper** (`PasskeySubmitTagHelper`) that renders a button which
  calls the WebAuthn API in the browser and posts the resulting credential JSON back to the server
  as a form field
- `PasskeyOperation.Create` and `PasskeyOperation.Request` — the two operations the tag helper drives
- a passkey management page at `/Account/Passkeys` where a signed-in user can add, rename and remove
  their own passkeys
- `AddAspNetIdentity<ApplicationUser>()` to bridge ASP.NET Core Identity into IdentityServer
- `AddLicenseSummary()`, which prints the license edition to the console at startup

> **Note:** passkeys require a **secure context**. `https://localhost` counts as one, so the sample
> works over local HTTPS, but it will not work over plain `http://` on another machine.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project AspNetIdentityPasskeys.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17237` and shows two resources: `identityserver`
   and `client`.

1. Click the highlighted **Seed Database** button on the `identityserver` resource and wait for it to
   report success. You must do this before signing in, or there will be no users.

   > Seeding calls `Database.Migrate()` only — it does **not** drop the database. That is
   > deliberate here: passkeys registered in a previous run survive a re-seed, so you can keep
   > experimenting with the credential you created.

1. Open a browser tab to the `client` application at `https://localhost:5002`. Click **Secure** in
   the navigation; you are redirected to the IdentityServer login page at `https://localhost:5001`.
1. Sign in with a **password** first: username `alice` (or `bob`) and password `Pass123$`. Note the
   username is what you type, not the email address.
1. You are returned to the client. Now sign out (the **Logout** button in the navigation) so you can
   exercise the passwordless flow.
1. On the login page, leave the username field filled in and click **Log in with a passkey**. The
   browser prompts you for the device's biometric or PIN. You are signed in without a password.
1. To register a passkey, sign in and go straight to `https://localhost:5001/account/passkeys` (there
   is a link to it on the IdentityServer home page). Click **Add a new passkey** and follow the
   browser prompt.
1. On that page you can also **Rename** a passkey to something friendlier or **Remove** it. Removing
   the last passkey does not lock you out — the password still works.
1. The `client` is a **confidential** client: it authenticates with the secret
   `49C1A7E1-0C79-4A89-A3D6-A37998FB86B0` in addition to the user, because it also holds a refresh
   token.

## Credentials

| Username | Email | Password |
|----------|-------|----------|
| `alice` | `AliceSmith@email.example` | `Pass123$` |
| `bob` | `BobSmith@email.example` | `Pass123$` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerAspNetIdentityPasskeys` | `https://localhost:5001` | Login, consent, passkey registration and sign-in |
| `Client` | `https://localhost:5002` | The web application that signs in |
| `AspNetIdentityPasskeys.AppHost` | `https://localhost:17237` | Aspire dashboard, with the **Seed Database** button |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)
- A browser that supports WebAuthn — any current Chrome, Edge, Safari or Firefox will do

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerAspNetIdentityPasskeys/HostingExtensions.cs` — the Identity schema version, the
  `ValidateOrigin` predicate and `MapPasskeyEndpoints()`
- `IdentityServerAspNetIdentityPasskeys/Passkeys/PasskeyEndpointRouteBuilderExtensions.cs` — the two
  WebAuthn option endpoints
- `IdentityServerAspNetIdentityPasskeys/Passkeys/PasskeySubmitTagHelper.cs` — the `<passkey-submit>`
  tag helper
- `IdentityServerAspNetIdentityPasskeys/wwwroot/js/passkey-submit.js` — the browser-side WebAuthn call
- `IdentityServerAspNetIdentityPasskeys/Pages/Account/Passkeys.cshtml` — the management UI
- `IdentityServerAspNetIdentityPasskeys/SeedData.cs` — the user seeding
