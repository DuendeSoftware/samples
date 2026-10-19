# Passkey Challenge Size

A Duende User Management sample showing generated challenge lengths, an explicit
64-byte configuration, and startup validation of the 16-byte minimum.

This sample uses public User Management preview `2.0.0-preview.6`, which includes startup validation
and is available from nuget.org.

## Run with 64-Byte Challenges

Requires .NET 10 and a trusted HTTPS development certificate. No authenticator, user account, Docker,
SMTP server, or external client application is needed.

Run from this directory:

```powershell
dotnet run
```

Alternatively, start the project from your IDE. No private package feed or NuGet credentials are required.

Open `https://localhost:5018`. **Configured size** is **64 bytes** because `appsettings.json` explicitly
sets `Passkeys:ChallengeSize` to `64`, overriding the library's **32-byte default**.
Click **Begin registration** or **Begin discoverable authentication**.
The page displays the challenge and its decoded byte length, not the number of base64url characters.
Repeated clicks generate fresh challenges.

This is a begin-only demonstration. Registration uses a fixed demonstration subject, not a signed-in
account. No passkey is created and no session is issued. The host does not map the User Management
completion endpoints. Do not deploy this local inspection page as an account-registration workflow.

## Compare with the 32-Byte Default

Stop the host and change `ChallengeSize` in `appsettings.json` to `32`:

```json
{
  "Passkeys": {
    "ChallengeSize": 32
  }
}
```

Restart the project from your IDE or with `dotnet run --no-restore`, then click either button.
Both **Configured size** and **Decoded challenge** show **32 bytes**.
`Program.cs` reads the optional setting and assigns it inside `authentication.Configure(...)`:

```csharp
options.Passkeys.ChallengeSize = challengeSize;
```

Set `ChallengeSize` back to `64` in `appsettings.json` and restart to restore the sample's configuration.
Remove `Passkeys:ChallengeSize` from `appsettings.json` and omit command-line/environment overrides
to use the library's **32-byte default** without explicitly configuring a size.
The option controls registration, discoverable authentication, and known-user authentication; all use the same challenge generator.

## Observe the Minimum and Startup Validation

Challenges need enough unpredictable randomness (entropy) to make guessing them infeasible and
prevent replay attacks. The [Web Authentication Level 3 specification](https://www.w3.org/TR/webauthn-3/#sctn-cryptographic-challenges)
states:

> In order to prevent replay attacks, the challenges MUST contain enough entropy to make guessing
> them infeasible. Challenges SHOULD therefore be at least 16 bytes long.

Duende User Management enforces **16 bytes as an absolute minimum** and uses **32-byte challenges
by default**. Any configured size must be at least **16 bytes**.
Stop the host, change `ChallengeSize` in `appsettings.json` to `16`, and restart.
Either button produces a 16-byte challenge.

Then stop the host, change the same value to `15`, and restart to try an intentionally invalid size.

The host fails to start with `OptionsValidationException` and the message:

```text
Passkeys.ChallengeSize must be >= 16, but was 15.
```

User Management validates `UserAuthenticationOptions` through `ValidateOnStart`; no page request
is needed to detect the invalid configuration. The ceremony-time guard remains as defense in depth,
not the expected configuration-validation boundary. Change the value back to `64` and restart
to continue using the sample.

Changing size does not change challenge timeout, challenge storage, supported
algorithms, attestation policy, or authentication requirements.

## Storage and Package Versions

The host uses the SQLite package's `AddSqliteInMemory()` demonstration/testing store and initializes
its schema with `IStorageInstanceSchema.MigrateAsync`. No database file is created.
Beginning a ceremony stores a challenge in memory even though this sample deliberately does not
complete it. All challenge data is discarded when the host stops. This store is not intended for
production use. No data import or migration of existing databases is needed.

| Package | Version |
|---------|---------|
| `Duende.IdentityServer` | `8.1.0-preview.6` |
| `Duende.Storage.Sqlite` | `2.0.0-preview.6` |
| `Duende.UserManagement.IdentityServer8` | `2.0.0-preview.6` |
| `Microsoft.Data.Sqlite` | `10.0.11` |

Duende previews use project-local `VersionOverride` entries. The direct SQLite provider reference
uses the existing central version instead of the preview's older transitive provider/native SQLite
dependency. Shared versions are unchanged. No custom cryptographic extension or challenge store is used.
The IdentityServer integration resolves `Duende.UserManagement` at `2.0.0-preview.6` transitively.
