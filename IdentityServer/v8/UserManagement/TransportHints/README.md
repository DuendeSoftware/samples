# Passkey Transport Hints

A Duende User Management sample showing how browser transport hints are captured at
registration, stored with a credential, and passed back to WebAuthn during known-user verification.
The independent [ChallengeSize](../ChallengeSize/README.md) sample covers challenge length.

A transport hint describes how a browser can communicate with an authenticator that holds a
credential. For example, `usb` and `nfc` describe external connection methods, while `internal`
indicates a platform authenticator built into the device, such as Windows Hello. A credential can
have multiple hints. During authentication, the browser can use these hints to guide authenticator
discovery and offer relevant ways to use the credential, rather than trying every transport.

**Transport hints work automatically.** There is no transport-hints setting to enable or tune in this
sample. The browser reports hints during registration; User Management stores them and returns them
during known-user authentication, and the stock JavaScript forwards them to WebAuthn.
The sample demonstrates this data flow, not a configurable with-hints versus without-hints comparison.
Unlike challenge size, the transport values are determined by the browser and authenticator, not
server configuration.

To explore different values, register different authenticators, such as Windows Hello and a USB
security key, using unique passkey names. Hints can help the browser tailor its authentication UI,
but a visibly different prompt is not guaranteed.

## Run the Sample

Requires .NET 10, a trusted HTTPS development certificate, and a WebAuthn-capable browser with a
platform authenticator or security key. No Docker, SMTP server, or external client application is needed.

Run this sample from a checkout of the samples repository. `TransportHints.csproj` links
`ConsoleOtpDispatcher.cs` and the `Login` and `EnterOtp` Razor pages and page models from the sibling
`GettingStarted` directory. These are build-time source dependencies, not calls to another running
application. Copying only the `TransportHints` directory will not produce a standalone build; keep
the repository structure, including `GettingStarted` and the shared build/package configuration.

This sample references public User Management preview `2.0.0-preview.6`, available from nuget.org.
Run from this directory:

```powershell
dotnet run
```

Alternatively, start the project from your IDE. No private package feed or NuGet credentials are required.

Open `https://localhost:5017`. Enter an email address, then use the OTP printed in the host's console
to sign in. Unknown email addresses are registered by the existing User Management OTP flow.

If changing the URL, also update `ServerDomain` and `AllowedOrigins` in `Program.cs`.

## Observe Registration and Stored Hints

1. Enter a unique passkey name and click **Register passkey**. Approve the browser's authenticator prompt.
2. Under **Follow the transport hints**, **Captured during registration** shows the hints submitted
   to `/passkeys/register/complete`, after the server accepts registration. The stock `/passkeys/js` calls
   `credential.response.getTransports()` when available and sends the result as `response.transports`.
   For example, a platform authenticator might report `["internal"]`; a security key might report
   `["usb", "nfc"]`. Results vary by browser and authenticator.
3. Optionally reload the page. The display clears, but the next step still reads the credential from
   the database, not from page state.

## Observe Known-User Forwarding

1. Click **Verify with known-user passkey**.
2. **Returned from storage** shows each credential's hints from
   `/passkeys/authenticate/begin`'s `options.allowCredentials`.
3. **Forwarded to WebAuthn** shows the hints passed to `navigator.credentials.get` by the stock
   JavaScript. Credential numbers correspond to the same positions in both lists. Missing transport
   hints are displayed as an empty array.
4. Approve the authenticator prompt to see **Passkey verified for the signed-in account**, or cancel
   if observing the handoff is sufficient. The observations remain visible after cancellation.

The page wraps `fetch` and `navigator.credentials.get` only to display transport metadata, then delegates
to the originals. It does not replace the stock ceremony logic or credential conversion, and does not
display challenges, assertions, or credential IDs. Developer tools remain available to inspect the full
requests. The three panels show the latest observations during this page visit, not a live database view.

Hints are optional, unverified browser UX information, not a transport allow-list, authenticator trust
decision, or proof of how an authenticator was used. Missing hints do not prevent credential use.
Unknown transport strings are preserved. Browser prompts are not guaranteed to change.
Discoverable authentication has an empty `allowCredentials` list, so this walkthrough deliberately
uses the known-user path to make per-credential hints observable.

## Application-Owned Verification

`SignedInUserResolver` implements `ISecondFactorPasskeyAuthenticationResolver` and returns the current
authenticated subject. Registration uses `authentication.EnablePasskeyForSecondFactor<SignedInUserResolver>()`
inside `AddUserManagement(...).Authentication(...)`. The interface and ceremony types are in
`Duende.UserManagement.Authentication.Passkeys`; the registration helpers use `Duende.UserManagement`
and `Duende.UserManagement.Authentication`.

Despite the registration method's name, this sample does **not** add an MFA requirement or a pending
first-factor flow. It verifies a passkey for an already signed-in user.

The stock `authenticateWithPasskey` calls `onSuccess` with a `PasskeyCompleteAuthenticationRequest`
payload; it does not submit completion or issue a session. The page POSTs that payload with a Razor
Pages antiforgery token to `OnPostVerifyAsync`, which calls
`IPasskeyCeremonies.CompleteAuthenticationAsync`. Only a
`PasskeyAuthenticationCompleteResult.Success` whose `UserSubjectId` matches the authenticated subject
is accepted. Verification neither replaces nor elevates the existing session.

Both registration and verification use the stock `onSuccess`/`onError` callback contract. An error is
shown on the page and controls are re-enabled instead of reporting a false success.

## Storage and Package Versions

The host uses the SQLite package's `AddSqliteInMemory()` demonstration/testing store and runs
`IStorageInstanceSchema.MigrateAsync` on startup. No database file is created or read.
It uses the integration's built-in profile schema; no custom schema bootstrap, import, or metadata
backfill is needed. Other samples and existing database files are untouched.

Users, registered credentials, transport hints, and challenges survive page reloads but are discarded
when the host stops. After restarting, clear this sample's browser cookies and repeat OTP sign-in
and passkey registration. Passkeys retained by your authenticator from an earlier run are no longer
registered in the sample's store. This in-memory store is not intended for production use.
Challenges retain the package's default size.

| Package | Version |
|---------|---------|
| `Duende.IdentityServer` | `8.1.0-preview.6` |
| `Duende.Storage.Sqlite` | `2.0.0-preview.6` |
| `Duende.UserManagement.IdentityServer8` | `2.0.0-preview.6` |
| `Microsoft.Data.Sqlite` | `10.0.11` |

Duende previews are pinned with project-local `VersionOverride` entries. The direct SQLite provider
reference uses the repository's existing central version, rather than the preview's older transitive
provider and native SQLite dependency. Shared package versions are unchanged.
This sample does not implement signature-verifier or attestation-format-validator extensions.
The IdentityServer integration resolves `Duende.UserManagement` at `2.0.0-preview.6` transitively.
