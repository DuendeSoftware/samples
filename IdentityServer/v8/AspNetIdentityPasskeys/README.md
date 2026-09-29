# ASP.NET Core Identity with passkeys sample

This sample shows how to register and sign in with **passkeys** (WebAuthn) using ASP.NET Core Identity
together with Duende IdentityServer. It is a full IdentityServer quickstart host (login, consent, device,
CIBA, grants, diagnostics pages) with the ASP.NET Core Identity store added on top.

### What the sample demonstrates

- `AddAspNetIdentity<ApplicationUser>()` combined with the standard IdentityServer quickstart configuration
- setting `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3`, which is required to persist passkeys
- `SignInManager.MakePasskeyCreationOptionsAsync` / `MakePasskeyRequestOptionsAsync` to produce WebAuthn options
- `PerformPasskeyAttestationAsync` / `AddOrUpdatePasskeyAsync` to register a new passkey
- `PasskeySignInAsync` for passwordless sign-in
- a `<passkey-submit>` custom element (Razor tag helper + `wwwroot/js/passkey-submit.js`) that calls
  `navigator.credentials.create` / `navigator.credentials.get` and posts the result back
- **conditional mediation** (passkey autofill) on the login page
- a passkey management UI to list, rename and remove passkeys

## How to Run

1. Seed the database. In the Aspire dashboard click **Seed Database** on the `identityserver`
   resource, or run the seed manually:

   ```bash
   dotnet run --project IdentityServerAspNetIdentityPasskeys -- /seed
   ```

   > **Note:** seeding only runs `Migrate()`, so registered passkeys survive a re-seed.

1. Run the Aspire project:

   ```bash
   dotnet run --project AspNetIdentityPasskeys.AppHost
   ```

1. Open a browser tab to the `client` application at `https://localhost:5002` and click **Secure**,
   or navigate directly to `https://localhost:5001`.
1. Log in as `alice` / `Pass123$` (or `bob` / `Pass123$`) using the password form.
1. Go to `https://localhost:5001/Account/Passkeys` and click **Add a new passkey**. Your browser will
   prompt you with the system authenticator (Windows Hello, Touch ID, Android screen lock, or a
   security key). You are then asked to give the passkey a name.
1. Back on the passkey list you can **Rename** or **Remove** the passkey.
1. Sign out and log back in using **Log in with a passkey** (or just use the username field and let
   passkey autofill do the work).

## Seeded users

| Username | Email | Password |
|----------|-------|----------|
| `alice` | `AliceSmith@email.example` | `Pass123$` |
| `bob` | `BobSmith@email.example` | `Pass123$` |

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerAspNetIdentityPasskeys` | `https://localhost:5001` |
| `Client` | `https://localhost:5002` |
| `AspNetIdentityPasskeys.AppHost` (Aspire dashboard) | `https://localhost:17237` |

## Prerequisites and caveats

- .NET 10 SDK and a trusted HTTPS development certificate (`dotnet dev-certs https --trust`).
  Passkeys require a **secure context**, so you must use `https://localhost:5001`.
- Passkeys are bound to the exact origin **`https://localhost:5001`**. This is configured in
  `HostingExtensions.cs` via `IdentityPasskeyOptions.ValidateOrigin`; if you change the port, update
  that check or passkeys will stop working.
- You need a **browser that supports WebAuthn Level 3 JSON serialization**
  (`PublicKeyCredential.parseCreationOptionsFromJSON` / `parseRequestOptionsFromJSON`) — current
  Chrome, Edge, Safari 17.4+ or Firefox 119+. Older browsers show "Some passkey features are missing."
- You need a machine with an **enrolled platform authenticator** (Windows Hello, Touch ID, Android
  screen lock) or a FIDO2 security key. A headless VM without one cannot complete the flow.
- The "Sign-in with demo.duendesoftware.com" button requires internet access; local passkey and
  password sign-in work offline.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerAspNetIdentityPasskeys/HostingExtensions.cs` — `AddAspNetIdentity`, `SchemaVersion = Version3`, `ValidateOrigin`
- `IdentityServerAspNetIdentityPasskeys/Passkeys/PasskeyEndpointRouteBuilderExtensions.cs` — the two WebAuthn option endpoints
- `IdentityServerAspNetIdentityPasskeys/Passkeys/PasskeySubmitTagHelper.cs` and `wwwroot/js/passkey-submit.js` — the custom element and WebAuthn calls
- `IdentityServerAspNetIdentityPasskeys/Pages/Account/Passkeys.cshtml.cs` — passkey management
- `IdentityServerAspNetIdentityPasskeys/Pages/Account/Login/Index.cshtml.cs` — password and passkey sign-in
