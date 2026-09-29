# Duende User Management getting started sample

This is the smallest possible IdentityServer that uses the
[`Duende.UserManagement`](https://www.nuget.org/packages/Duende.UserManagement) package. It shows
passwordless, one-time-password (OTP) sign-in backed by a local SQLite user store.

### What the sample demonstrates

- `builder.AddUserManagement(options => options.AddSqliteStore(...))` — enabling user management with
  SQLite persistence
- `IDatabaseSchema.MigrateAsync(...)` at startup, so the schema is created automatically on first run
- `IOtpSender.TrySendOtpAsync(...)` to issue a one-time code
- `IOtpAuthenticator.TryAuthenticateAsync(...)` to verify it and sign the user in
- a **swappable OTP transport**: `IOtpDispatcher` is implemented by `ConsoleOtpDispatcher`, which writes
  the code to the console instead of sending email
- `IIdentityServerInteractionService.IsValidReturnUrl(...)` to validate the `returnUrl` before signing in
- `AddDataProtection().SetApplicationName(...)` so the data protection key ring is stable across restarts

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

There is no Aspire AppHost, no Docker and no SMTP server involved.

## How to Run

> [!IMPORTANT]
> Run this from a **terminal you can read**. The one-time code is printed to the console by
> `ConsoleOtpDispatcher`; no email is sent. If you started the app with `launchBrowser` from an IDE,
> check the IDE's output window.

1. Run the project:

   ```bash
   dotnet run --project GettingStarted.csproj
   ```

1. The browser opens at `https://localhost:5001`. On first run the schema is migrated and
   `usermanagement.db` is created next to the project.
1. On the home page, click the link to the **Login** page.
1. Enter **any** email address (for example `alice@example.com`) and submit.
1. Look at the terminal — it prints something like:

   ```text
   OTP for alice@example.com: 123456
   ```

1. Enter that code on the **Enter OTP** page.
1. You are signed in. At this point the user profile is created in the database.

There are no usernames or passwords to configure: authentication is passwordless, and a profile is
created on the fly the first time an address is verified.

## Projects and URLs

| Project | URL |
|---------|-----|
| `GettingStarted` | `https://localhost:5001` — **open this one** |

> **Note:** this project listens on `5001`, which is the same port used by the IdentityServer hosts of the
> other quickstarts. The two cannot run at the same time. `Config.cs` also declares a client `interactive`
> with a redirect URI of `https://localhost:5002/signin-oidc`, which is a configuration artifact for
> driving the interactive client from a separate application; this project does not host that client
> itself, so a successful login will usually end at a redirect to `https://localhost:5002`.

## Notes on the data

- `usermanagement.db` is a local runtime artifact, not a seed. It is gitignored, and since there is no
  seeder **no users exist until you complete an OTP verification for an address**.
- To start from scratch, delete `usermanagement.db` and run the app again.
- `SetApplicationName("IdentityServer-UserManagement-GettingStarted")` keeps the data protection keys
  consistent across restarts. On Linux and macOS, configure a persistent key storage location, otherwise
  keys are regenerated on every run and existing sessions stop working.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `Program.cs` — `AddUserManagement`, the SQLite store, `ConsoleOtpDispatcher` registration and `MigrateAsync`
- `ConsoleOtpDispatcher.cs` — the OTP transport, which writes to the console
- `Config.cs` — the `interactive` client
- `Pages/Account/Login.cshtml.cs` — issuing the OTP and validating the return URL
- `Pages/Account/EnterOtp.cshtml.cs` — verifying the code

For richer examples, see the other samples in this directory:
[`FullSample`](../FullSample), [`AccountLockout`](../AccountLockout) and
[`PasswordRegistration`](../PasswordRegistration).
