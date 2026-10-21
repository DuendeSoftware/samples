# Duende User Management SCIM Provisioning Sample

This sample connects two applications through SCIM 2.0:

- **ScimProvisioning.Directory** (source) is a simulated upstream directory. It stands in for an identity provider or HR system such as Entra ID or Okta, and pushes employees and departments with a small hand-written SCIM client.
- **ScimProvisioning.IdentityServer** (destination) is Duende IdentityServer with User Management's inbound SCIM endpoints enabled. It stores the provisioned users and groups, and lets provisioned users sign in with an email one-time password (OTP).

> [!IMPORTANT]
> Duende User Management implements **inbound** SCIM only. The Directory project is not a Duende feature. It exists to show what a SCIM client sends.

> [!NOTE]
> SCIM support is experimental in Duende User Management `2.0.0-preview.6`. The sample opts in with `#pragma warning disable duende_experimental`.

## Running the sample

### Prerequisites

- .NET 10 SDK
- Docker (for Mailpit via Aspire)

### Start with Aspire

```bash
cd ScimProvisioning.AppHost
dotnet run
```

| Service | URL |
|---------|-----|
| Directory (SCIM source) | `https://localhost:6212` |
| IdentityServer (SCIM destination) | `https://localhost:6211` |
| Mailpit UI | `http://localhost:8028` |
| Aspire dashboard | `https://localhost:17132` |

The AppHost passes the IdentityServer URL to the Directory as both the token authority and the SCIM base URL (`/scim/`).

No license key is required for local development. IdentityServer logs license warnings, including one for SCIM 2.0 Inbound, but the SCIM endpoints and sign-in work.

## Walkthrough

1. Open the Directory and select **Discover SCIM capabilities** to read `ServiceProviderConfig`, `Schemas` and `ResourceTypes`. These metadata endpoints are anonymous.
2. Select an Engineering employee and **POST user**. The Directory sends `POST /scim/Users` with `userName`, `externalId`, `displayName`, `active` and `emails`.
3. Provision the other Engineering employee, then select Engineering and **POST group with members**. The Directory resolves each user's SCIM id by `userName` and sends them with `POST /scim/Groups`.
4. Select an Engineering employee and **PATCH active=false** to store a change to the SCIM profile.
5. Run **Bulk create Research employees and group** to create the Research employees and the Research group in one `POST /scim/Bulk` request. The group's members reference the new users with `bulkId:` values.
6. Open IdentityServer to see the stored users (including `active`) and groups.
7. Select **OTP sign-in**, enter a provisioned `userName`, and read the code from Mailpit. After sign-in, the home page shows the session's subject, which equals the user's SCIM `id`.

Every Directory action prints a short result with status codes and ids. The sample never logs access tokens, client secrets, OTP codes or full SCIM response bodies.

## How the destination is configured

`ScimProvisioning.IdentityServer/Program.cs`:

- Replaces the User Management profile schema with `DefaultScimUserSchema`. SCIM users are keyed on `userName`, which the default profile schema does not have.
- Calls `AddUserManagement(users => users.EnableScim(...))` and `app.MapScim()`.
- Registers `ScimResources.ApiResource`, `ScimResources.FullAccessScope` (`scim`) and `ScimResources.ReadOnlyScope` (`scim.read`).
- Sets `IssuerUri` and adds two client credentials clients:
  - `scim-provisioner` with scope `scim`, used by the Directory
  - `scim-reader` with scope `scim.read`, for trying read-only access

With the default SCIM authorization, writes require scope `scim`, reads accept `scim` or `scim.read`, and tokens must carry the `urn:duende:scim` audience. Requests without a token get `401` and a `scim.read` token gets `403` on writes.

### Known limitations in 2.0.0-preview.6

- **Bearer token signing-key discovery.** The `DuendeScimBearer` JWT bearer scheme receives its `Authority` too late to discover signing keys, so valid tokens are rejected with `401 invalid_token`. The sample sets the authority directly:

  ```csharp
  builder.Services.Configure<JwtBearerOptions>("DuendeScimBearer", options => options.Authority = issuer);
  ```

  Remove this once the product fix ships.
- **Looking profiles up by `userName`.** `IUserProfileAdmin.TryGetAsync(DefaultScimUserSchema.UserName.Code, value)` does not find SCIM-provisioned profiles. The login page uses a filtered `QueryAsync` and requires exactly one exact match instead.
- **`externalId` is not returned.** The Directory sends `externalId`, but the destination does not return it on users or groups. The Directory doesn't implement reconciliation; it resolves user ids by `userName` only to create a group with members.

## Signing in as a provisioned user

The login page only signs in users that were provisioned through SCIM:

- It looks up the profile by `userName` and refuses unknown names. OTP just-in-time registration is not used, so a failed lookup can never create a second profile.
- It attaches the email OTP address to that existing profile when the profile has no authenticator registration yet. If the profile already has a different registration, the request is refused.
- After the code is verified, it checks that the authenticated subject equals the provisioned profile's subject before issuing a session.

In this demo the `userName` is the user's email address. A real deployment would decide how SCIM attributes map to authenticators.

## `active` is profile data

SCIM `active` is stored as a profile attribute. This sample does **not** block sign-in or revoke sessions when `active` is `false`. A user deactivated in the Directory can still sign in.

Treat `active` as data your application can act on, for example:

- refusing to issue a session after authentication succeeds
- revoking existing sessions or refresh tokens when the value changes
- hiding the user from application features

## Sample shortcuts

- Client secrets are hard-coded in `appsettings.json` and `Program.cs`. Use a secret store in real deployments.
- All destination storage is in memory: User Management profiles and groups use an in-memory SQLite database, clients and resources use the in-memory stores, and the signing key is a non-persisted developer key with automatic key management turned off. Nothing survives a restart. Restart the whole AppHost rather than the destination alone, because the Directory caches its access token and the destination's new signing key will not validate it.
- The Directory's employee list is seeded in memory, so repeated **POST user** or **Bulk** calls return `409` once the users exist. Restart the AppHost to start again.
- The destination's users and groups pages are read-only demonstration views, not administration UI.
