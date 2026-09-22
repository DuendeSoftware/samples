# Duende User Management Account Lockout Sample

This standalone sample shows one way for a customer application to implement **full-account lockout policy** on top of Duende User Management by storing lockout state in **customer-owned profile attributes** and enforcing that policy **only after OTP or passkey authentication succeeds**.

> [!IMPORTANT]
> `account_locked_until` and `account_locked_indefinitely` in this sample are **application-defined business data**. They are **not canonical Duende User Management authenticator state**.

## What the sample demonstrates

- OTP and discoverable passkeys as **alternative** sign-in methods
- Mailpit-delivered OTP codes launched by Aspire
- Custom profile attributes for temporary and indefinite account lockout
- Post-authentication lockout enforcement for both OTP and passkey sign-in
- An admin-only user list that can apply these lockout presets:
  - 15 minutes
  - 1 hour
  - custom UTC expiry
  - indefinite lockout
  - unlock
- Prevention of self-lockout for the currently signed-in administrator

## Why lockout is enforced after authentication

This sample **does not** check or disclose lockout during the OTP request step or before passkey verification. Doing so would create an account-enumeration signal:

- “this identifier exists and is locked”
- “this identifier exists and is unlocked”
- “this identifier does not exist”

Instead, the sample lets OTP or passkey authentication succeed first, then applies the application lockout policy before issuing the authenticated session cookie. At that point the UI can safely disclose whether the account is locked temporarily or indefinitely, because the user has already proven control of a real authenticator.

## Full-account lockout vs per-authenticator throttling

This sample models **full-account policy**. If the lock is active, the account is blocked even when OTP or passkey authentication succeeds.

That is different from **per-authenticator throttling** such as OTP request throttles, OTP verification throttles, or passkey-specific anti-abuse controls. Those can still exist alongside this sample, but they solve a different problem.

## Running the sample

### Prerequisites

- .NET 10 SDK
- Docker (for Mailpit via Aspire)

### Start with Aspire

```bash
cd AccountLockout.AppHost
dotnet run
```

This launches:

| Service | URL |
|---------|-----|
| Account Lockout sample | `https://account-lockout.dev.localhost:6011` |
| Mailpit UI | `http://localhost:8027` |
| Mailpit SMTP | `smtp://localhost:1027` |
| Aspire dashboard | `https://aspire.dev.localhost:*` |

The web project uses the repository's Aspire service defaults, so its structured logs, distributed traces, runtime and HTTP metrics, and development health checks are available from the Aspire dashboard.

### Seeded identities

| User | Email | Notes |
|------|-------|-------|
| Sample Administrator | `admin@duendesoftware.com` | Can access `/Admin/Users` |
| Alice Sample User | `alice@duendesoftware.com` | Standard user for lock/unlock demos |

Both start with email OTP enabled. Sign in once with OTP, retrieve the code from Mailpit, and then register a passkey for future passwordless sign-in.

The OTP flow also supports just-in-time registration. Entering a new email address creates its user profile only after the OTP has been verified successfully.

## Admin workflow

1. Sign in as `admin@duendesoftware.com` using OTP.
2. Open **Users**.
3. Lock `alice@duendesoftware.com` with a preset, a custom UTC expiry, or an indefinite lock.
4. Sign in as Alice with OTP or a passkey.
5. Authentication succeeds, but the application blocks session issuance and shows the lock result.
6. Unlock Alice and sign in again successfully.

The current signed-in administrator cannot lock their own account.

## Passkey notes

The sample is configured for:

- **RP ID / server domain**: `account-lockout.dev.localhost`
- **Allowed origin**: `https://account-lockout.dev.localhost:6011`

If you change the HTTPS launch URL, update the `Sample:PublicOrigin` setting in `appsettings.json` so passkeys continue to work.

## Customer extensibility ideas

These profile attributes are just one sample boundary. In a real deployment you could replace them with:

- SIAM or fraud-engine decisions
- device-risk or impossible-travel evaluation
- support or compliance holds
- tenant-specific account-policy services
- different disclosure rules or support escalation flows

The important design point is keeping **application business policy** separate from canonical authenticator records unless your product explicitly wants those concerns coupled.
