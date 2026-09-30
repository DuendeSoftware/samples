# Custom Profile Service sample

By default IdentityServer issues the claims listed in the `UserClaims` of the identity resources and
API scopes you request, filtered against the claims present in the user's authentication session.
That is often not enough — you may need to look something up, or issue a claim only for a particular
client. A custom `IProfileService` is the extension point for that.

This sample registers a profile service and shows several ways to shape the claims in the response.

### What the sample demonstrates

- `AddProfileService<CustomProfileService>()`, replacing the default profile service
- `context.AddRequestedClaims(context.Subject.Claims)` — **OPTION 1A**: copy claims out of the user's
  session cookie, filtered to the ones actually requested
- `context.AddRequestedClaims(user.Claims)` — **OPTION 1B**: the same, but sourced from the user store
  instead of the session
- `context.IssuedClaims.Add(...)` — **OPTION 2**: emit a claim unconditionally, ignoring what was
  requested
- adding a `tenant` claim based on the calling client, checked with
  `if (context.Client.ClientId == "client1")` — the standard way to vary the token contents per
  client
- adding a `foo` claim based on `context.Caller == IdentityServerConstants.ProfileDataCallers.ClaimsProviderAccessToken`,
  which distinguishes the access token from the id token and the userinfo endpoint
- reading `context.Subject.Claims` for hints already on the session, such as a `picture` claim
- `identity.mvc.sample` style: the client is registered as `interactive.mvc.sample` with the
  `secret` client secret, and requests `openid`, `email`, `scope1` and `offline_access`
- `MapDefaultEndpoints()`-style API access through the shared `SimpleApi` project, so you can see the
  resulting access token in use
- a shared `Shared\Constants.cs` holding the `Urls` constants for the IdentityServer and API, linked
  into the client project so the ports are defined once

> **Note:** the `SimpleApi` project's authorization policy requires the `scope` claim to contain
> `SimpleApi`, but the `SimpleApi` scope is not defined in IdentityServer and the policy is **not**
> applied (`app.MapControllers().RequireAuthorization("SimpleApi")` is commented out). The API
> therefore accepts any access token from the authority — which is what lets this sample work
> without registering an API scope. Do not copy that leniency into production.

> **Known quirk:** the `tenant` claim is guarded by `context.Client.ClientId == "client1"`, but the
> only client registered in `Clients.cs` is `interactive.mvc.sample`. The `tenant` claim therefore
> never appears. Change the guard to `"interactive.mvc.sample"` to see it.

> **Known quirk:** the sample also registers an OIDC handler named **Sign-in with Google** with a
> client id but **no client secret**. Google's authorization code flow for a server-side web app
> requires a secret, so that button will not complete.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project ProfileService.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17047` and shows three resources:
   `identityserverhost`, `simpleapi` and `client`.

1. Open a browser tab to the `client` application at `https://localhost:44300`. You are anonymous;
   the home page shows an empty claim list.
1. Click **Secure** in the navigation; you are redirected to the IdentityServer login page at
   `https://localhost:5001`.
1. Log in with `alice` / `alice` (or `bob` / `bob`) and approve the request.
1. You are returned to the client. The home page lists the claims in your session.
1. Click **Secure** in the navigation. That page has two buttons:
   - **Call API** — calls `GET https://localhost:5002/identity` on the `SimpleApi` project with the
     access token. The API echoes back the claims from the token, which is where you see the effect
     of the profile service on the **access token**.
   - **Refresh token** — performs a manual refresh token grant with
     `RequestRefreshTokenAsync`, exchanging the refresh token for a new access token and persisting
     the rotated refresh token back into the session.
1. Compare the access token claims with the session claims on the home page. Any claim the profile
   service added appears in the API response.
1. To see the `tenant` claim appear, change the guard in `CustomProfileService.cs` from `"client1"`
   to `"interactive.mvc.sample"`, restart the IdentityServer and repeat. `foo` appears in the access
   token only, because it is guarded on `ProfileDataCallers.ClaimsProviderAccessToken`.
1. Try the **Sign-in with Google** button on the login page to see the missing-secret failure
   described above.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerHost` | `https://localhost:5001` | Login, consent and token issuance |
| `Client` | `https://localhost:44300` | The web application that signs in and calls the API |
| `SimpleApi` | `https://localhost:5002` | Protected API, echoes the token claims |
| `ProfileService.AppHost` | `https://localhost:17047` | Aspire dashboard |

> `SimpleApi` is shared with the other samples — this one is at `IdentityServer/v7/Apis/SimpleApi`.

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

No database or seeding step is required — the configuration is in-memory and the test users are
registered with `AddTestUsers`.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/CustomProfileService.cs` — the entire sample, including all three approaches
  side by side
- `IdentityServerHost/Program.cs` — `AddProfileService<CustomProfileService>()`
- `IdentityServerHost/Clients.cs` — the client registrations, and the source of the `client1`
  mismatch
- `Client/Shared/Constants.cs` — the shared `Urls` constants
- `..\..\Apis\SimpleApi\Program.cs` — the (disabled) `SimpleApi` scope policy

For the ASP.NET Core Identity variant of a profile service, see the
[`5_AspNetIdentity`](../Quickstarts/5_AspNetIdentity) quickstart.
