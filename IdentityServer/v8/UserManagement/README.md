# Duende User Management Sample

This sample demonstrates a complete IdentityServer v8 implementation using **Duende User Management** — a user store and authentication platform that ships as a NuGet package and replaces ASP.NET Identity for IdentityServer scenarios.

## What This Sample Shows

### Authentication Methods

| Method | Flow |
|--------|------|
| **Email OTP** | Enter email → receive code via SMTP → verify → sign in (auto-registers unknown emails) |
| **Password + TOTP 2FA** | Enter email/password → if TOTP device registered, prompted for code or passkey |
| **Passkeys** | After first OTP login, user is prompted to register a passkey for future passwordless sign-in |
| **Passkey as second factor** | After password verification, user can tap a passkey instead of entering a TOTP code |
| **Google external login** | OAuth callback creates/links a local profile automatically |

### User Profile Management

User Management uses a **schema-driven attribute model** rather than a fixed user table. Attributes like `email`, `name`, `website`, and custom ones like `location` are defined as `AttributeDefinition` entries. Profiles are collections of `AttributeValue` instances tied to a `UserSubjectId`.

### Migration from ASP.NET Identity

The Admin → Import page demonstrates bulk-importing users from an existing ASP.NET Identity SQLite database, including:

- Password hash compatibility (imports hashes as-is using a custom `IPasswordHashAlgorithm`)
- Claims-to-attributes mapping (e.g., `given_name` + `family_name` → `name`)
- Deterministic subject ID generation for idempotent re-imports
- Conflict resolution (overwrite strategy)

### Second Factor State

When password authentication succeeds and the user has a TOTP device, the subject ID is stored in a short-lived encrypted cookie (`SecondFactorStateCookie`). Both the TOTP verification page and the passkey-as-second-factor flow read from this cookie to know which user is completing 2FA.

## Running the Sample

### Prerequisites

- .NET 10 SDK
- Docker (for Mailpit email testing via Aspire)

### Start with Aspire

```bash
cd UserManagementSample.AppHost
dotnet run
```

This launches:

| Service | URL |
|---------|-----|
| IdentityServer | `https://localhost:5001` |
| Client App | (assigned by Aspire) |
| Mailpit UI | `http://localhost:8025` |
| Mailpit SMTP | `localhost:1025` |
| Aspire Dashboard | `https://localhost:15027` |

### Test Credentials

In Development mode, a test credentials card appears on the login page:

| User | Email | Password | Auth Method |
|------|-------|----------|-------------|
| Alice | `alice@duendesoftware.com` | `BadPassword123!#` | Password |
| Bob | `bob@duendesoftware.com` | — | OTP (check Mailpit) |

### Passkey Configuration

Passkeys require matching domain/origin configuration. The sample is configured for:

- **Server domain**: `localhost`
- **Allowed origin**: `https://localhost:5001`

If you change the hosting URL, update these values in `Program.cs`.

### Google External Login

Google authentication is configured but hidden when credentials are not present. To enable it, add your Google OAuth client ID and secret to the app configuration.

## Project Structure

```
UserManagement/
├── UserManagementSample/              # IdentityServer application
│   ├── Program.cs                     # Service registration and configuration
│   ├── SeedData.cs                    # Creates test users on startup
│   ├── SecondFactorStateCookie.cs     # Encrypted cookie for 2FA interim state
│   ├── OtpCookie.cs                   # Encrypted cookie for OTP flow state
│   ├── Pages/
│   │   ├── Account/                   # Login, OTP, password, passkey, external
│   │   ├── Manage/                    # Profile, 2FA setup, passkey management
│   │   └── Admin/                     # User search, details, import
│   ├── Import/                        # ASP.NET Identity migration logic
│   └── Services/                      # SecondFactorResolver for passkey 2FA
├── UserManagementSample.AppHost/      # Aspire orchestrator (Mailpit + services)
├── UserManagementSample.Client/       # OIDC client app (Authorization Code + PKCE)
├── UserManagementSample.AspNetIdentitySource/  # Legacy identity DB for import demo
└── UserManagementSample.ServiceDefaults/       # Shared Aspire configuration
```

## Key APIs Demonstrated

### Configuration (Program.cs)

```csharp
builder.Services
    .AddIdentityServer()
    .AddUserManagement(um =>
    {
        um.Authentication(auth =>
        {
            auth.Configure(opt =>
            {
                opt.Passwords.MinLength = 8;
                opt.Passkeys.ServerDomain = "localhost";
                opt.Passkeys.AllowedOrigins = ["https://..."];
            });

            auth.EnablePasskeyForSecondFactor<SecondFactorResolver>();
            auth.UseSmtpOtpDispatcher(smtp => { ... });
        });

        um.AddSqliteStore(opt =>
            opt.ConnectionString = "Data Source=../db/usermanagement.db");
    });
```

### OTP Authentication

```csharp
// Send OTP
var result = await otpSender.TrySendOtpAsync(
    new OtpAddress(OtpChannel.Email, EmailAddress.Create(email)), ct);

// Verify OTP
var authResult = await otpAuthenticator.TryAuthenticateAsync(
    PlainTextOtp.Create(code), token, ct);
```

### Password Authentication with 2FA Check

```csharp
// Verify password
var result = await passwordAuthenticator.TryAuthenticateAsync(
    attributeCode, email, NonValidatedPassword.Create(password), ct);

// Check for TOTP devices
var authenticators = await selfService.TryGetAsync(subjectId, ct);
if (authenticators.TotpDeviceNames.Any())
{
    secondFactorStateCookie.Write(subjectId);
    return RedirectToPage("LoginWith2FA");
}
```

### TOTP Setup

```csharp
// Generate key and provisioning URI
var key = PlainBytesTotpKey.New();
var uri = TotpAuthenticatorUri.Generate("Issuer", account, key);

// Register device after user verifies a code
await selfService.TryAddTotpDeviceAsync(
    subjectId, TotpDeviceName.Default, key, totp, ct);
```

### External Authentication

```csharp
// Single call handles lookup-or-create
var result = await externalAuthenticator.TryAuthenticateAsync(
    new ExternalAuthenticatorAddress(
        ExternalAuthenticatorName.Create(provider),
        OpaqueSubjectId.Create(externalSubjectId)), ct);
```

### User Import

```csharp
var records = users.Select(u => new UserImportRecord(
    UserSubjectId.Create(deterministicGuid),
    new AttributeValueCollection(schema) { ["email"] = u.Email, ["name"] = u.Name },
    new AuthenticatorImport(
        Password: new PasswordImport(hashData),
        OtpAddresses: [new OtpAddress(OtpChannel.Email, EmailAddress.Create(u.Email))]
    )
)).ToArray();

await userImporter.ImportAsync(records, ct);
```

## Packages

| Package | Version |
|---------|---------|
| `Duende.IdentityServer` | 8.0.0 |
| `Duende.UserManagement.IdentityServer8` | 1.0.0 |
