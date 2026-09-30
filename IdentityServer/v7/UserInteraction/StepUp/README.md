# Step-Up Authentication sample

[Step-up authentication](https://datatracker.ietf.org/doc/draft-ietf-oauth-step-up-authn-challenge)
lets a resource ask for a *stronger* authentication than the user has already proven. A banking API
might accept an existing session for viewing a balance, but demand a fresh MFA challenge before
showing account details.

It is built on two standard OpenID Connect / OAuth 2.0 mechanisms:

- **`acr_values`** on the authorization request — the client asks for a specific authentication
  context, here `mfa`
- **`max_age`** on the authorization request — the client asks how recently the user must have
  authenticated

The API signals its requirements back with a `WWW-Authenticate` response header, and the client
turns that into a challenge to the IdentityServer. This sample implements the full round trip.

### What the sample demonstrates

**The API** (`Api`, on port 7001)

- endpoints with different authentication requirements, in `StepUpController.cs`
- four endpoints under `/step-up`, each behind a different policy: `/neither` (`[Authorize]`,
  just needs a signed-in user), `/max-age` (`MaxAgeOneMinute`), `/mfa` (`MfaRequired`) and `/both`
  (`RecentMfa`)
- a `MaxAgeRequirement` / `MaxAgeRequirementHandler` that reads the `auth_time` claim and succeeds
  while `DateTime.UtcNow - authTime` is under the requirement's `MaxAge`
- `StepUpAuthorizationMiddlewareResultHandler`, registered as an
  `IAuthorizationMiddlewareResultHandler`. When a policy fails, it inspects the failed requirements
  and writes a `StepUpWWWAuthenticateHeader` — its own `WWW-Authenticate` implementation that can
  carry both `max_age` and `acr_values`
- `header.AcrValues = "mfa"` and `header.MaxAge = (int)maxAgeReq.MaxAge.TotalSeconds` — the API
  telling the client exactly what it needs
- the endpoints echoing back the authentication age and the `amr` values, so you can see *why* the
  request was accepted or denied

**The client** (`Client`, on port 6001)

- `StepUpHandler`, a `DelegatingHandler` on the API `HttpClient` that parses the
  `WWW-Authenticate` header, and on a step-up response re-challenges the OIDC handler with the
  corresponding `acr_values` / `max_age` values
- `ctx.ProtocolMessage.AcrValues = ctx.Properties.Items["acr_values"]` and
  `ctx.ProtocolMessage.MaxAge = ctx.Properties.Items["max_age"]` in an
  `OpenIdConnectEvents.OnRedirectToIdentityServer` handler, which is where the values are put back
  onto the authorize request
- pages for each requirement, matching the API endpoints: **Secure**, **Recent Auth**, **MFA** and
  **Recent Auth with MFA**

**The IdentityServer** (`IdentityServerHost`, on port 5001)

- `StepUpInteractionResponseGenerator`, registered as an `IAuthorizeInteractionResponseGenerator` and
  derived from `AuthorizeInteractionResponseGenerator`
- it asks two questions in `MfaRequired(request)`: did the client ask for it
  (`request.AuthenticationContextReferenceClasses.Contains("mfa")`), or does this user always need it
  (`AlwaysUseMfaForUser`, which here is hard-coded to `sub == "bob"`)? If MFA is required and
  `AuthenticatedWithMfa` finds no `amr == "mfa"` claim on the subject, it redirects to a custom
  `/Account/Mfa` page
- if the user has already declined — the `declined_mfa` claim is present — it returns
  `OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements` instead, so the browser is not
  bounced into a re-prompt loop
- a fake MFA page at `Pages/Account/Mfa/Index.cshtml` that appends
  `JwtClaimTypes.AuthenticationMethod = "mfa"` to the user's claims and signs them back in
- `AcrDiscoveryDocumentGenerator`, registered as an `IDiscoveryResponseGenerator`, which advertises
  `acr_values_supported: ["1"]` in the discovery document so clients know MFA is available
- an `AddOpenIdConnect("oidc", "Sign-in with demo.duendesoftware.com", ...)` handler, so the login
  page also offers the public demo server as an external provider

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project StepUp.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17202` and shows three resources:
   `identityserverhost`, `api` and `client`.

1. Open a browser tab to the `client` application at `https://localhost:6001`. Click **Secure**; you
   are redirected to the IdentityServer login page at `https://localhost:5001`.
1. Log in as `alice` with password `alice`. Alice does **not** require MFA to log in, so you are
   returned to the client immediately.
1. Click **MFA**. The client calls `GET /step-up/mfa` on the API. The API rejects it with
   `WWW-Authenticate: acr_values=mfa`; the client's `StepUpHandler` intercepts that and re-challenges
   the IdentityServer with `acr_values=mfa`.
1. The IdentityServer's `StepUpInteractionResponseGenerator` sees the request needs MFA and Alice has
   no `amr: mfa` claim, so it redirects to the fake MFA page at `/Account/Mfa` instead of the home
   page. Approve it.
1. You are returned to the client and the API call now succeeds. The page shows how long ago you
   authenticated and which `amr` values were used.
1. Click **Recent Auth** to make a call to `/step-up/max-age`, which requires authentication in the
   past minute. The API responds with `max_age` and the client re-challenges with that parameter.
   Because you just authenticated, the call may succeed immediately; wait a minute and try again to
   watch the step-up actually trigger.
1. Click **Recent Auth with MFA** to call `/step-up/both`, sending `acr_values` *and* `max_age`
   together.
1. Sign out and sign in as `bob` with password `bob`. Bob always requires MFA — that is the
   `AlwaysUseMfaForUser` special case — so even the initial **Secure** sign-in is challenged.
   Compare that with Alice.
1. Decline an MFA challenge once. The fake MFA page records a `declined_mfa` claim, and the
   generator then returns `UnmetAuthenticationRequirements` rather than redirecting you back, so you
   land on the client's error page instead of an endless re-prompt. There is a dedicated
   **MfaDeclined** page in the client for this.

## Credentials

| Username | Password | Behaviour |
|----------|----------|-----------|
| `alice` | `alice` | Does not require MFA to log in |
| `bob` | `bob` | Always requires MFA |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerHost` | `https://localhost:5001` | Login, the custom MFA page, `acr_values` handling |
| `Client` | `https://localhost:6001` | The web application that triggers step-up |
| `Api` | `https://localhost:7001` | Protected resource that demands stronger authentication |
| `StepUp.AppHost` | `https://localhost:17202` | Aspire dashboard |

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

- `Api/Authorization/StepUpHandler.cs` — the `IAuthorizationMiddlewareResultHandler` and the
  `WWW-Authenticate` header
- `Api/Authorization/MaxAgeHandler.cs` — the `auth_time` check
- `Api/Controllers/StepUpController.cs` — the endpoints with their differing requirements
- `Client/StepUpHandler.cs` — the client-side `DelegatingHandler` that re-challenges
- `IdentityServerHost/StepUpInteractionResponseGenerator.cs` — deciding when to show the MFA page
- `IdentityServerHost/AcrDiscoveryDocumentGenerator.cs` — advertising `acr_values_supported`
- `IdentityServerHost/Pages/Account/Mfa/Index.cshtml` — the fake MFA page

> **Note:** `IdentityServerHost/README.md` in this folder is an older, project-by-project
> walkthrough of the same sample. It covers the same ground — the `WWW-Authenticate` header, the
> `acr_values` / `max_age` parameters and the custom UI — and is a good complement if you want the
> narrative rather than the API surface. It tells you to start the three projects individually;
> this README uses the Aspire AppHost, which is the current convention.

For CIBA, which solves a related problem for native and desktop applications that cannot host a
browser, see the sibling [`Ciba`](../Ciba) sample.
