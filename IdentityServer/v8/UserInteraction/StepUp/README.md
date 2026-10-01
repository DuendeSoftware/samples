# Step-Up authentication sample

This sample shows how to implement
[Step-Up authentication](https://datatracker.ietf.org/doc/draft-ietf-oauth-step-up-authn-challenge)
with Duende IdentityServer, so a resource server can demand a stronger level of authentication than the
user currently has.

### What the sample demonstrates

- a resource server that returns a `WWW-Authenticate` challenge describing which authentication
  requirements were not met (MFA, recent authentication, or both)
- a client that turns that challenge into an authorization request carrying `acr_values` and/or `max_age`
- IdentityServer honouring `max_age` (which the default `AuthorizeInteractionResponseGenerator` already
  does) and `acr_values` via a **custom** `IAuthorizeInteractionResponseGenerator` that chooses which page
  to show the user
- custom UI pages that explain to the user what is happening during step-up
- the `Recent Auth` and `Recent Auth with MFA` pages, and a user (`bob`) who always requires MFA

### Projects

| Project | Role |
|---------|------|
| `IdentityServerHost` | the token server |
| `Api` | a protected resource that can issue step-up challenges when a request does not meet its requirements |
| `Client` | a client application that logs in and calls the API |

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project StepUp.AppHost
   ```

1. Open a browser tab to the client at `https://localhost:6001`.
1. Click on the **secure** page. This triggers login. Authenticate with `alice` / `alice` — note that
   alice does **not** require MFA to log in.
1. Click the **MFA** page to make an API request that requires MFA. This triggers step-up: alice is shown a
   (deliberately fake) MFA page at IdentityServer before being returned to the client.
1. Finally, click the **Recent Auth** page to make an API request that requires an authentication in the
   past minute. The page shows the age of the current authentication.
   - You may need to refresh the page after a minute has passed in order to trigger step-up again.
1. From there you can experiment further: try the **Recent Auth with MFA** page, which has both
   requirements, or sign in as `bob`, who always requires MFA.

## Seeded users

| Username | Password | MFA behaviour |
|----------|----------|---------------|
| `alice` | `alice` | does not require MFA at login |
| `bob` | `bob` | always requires MFA |

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerHost` | `https://localhost:5001` |
| `Client` | `https://localhost:6001` — **open this one** |
| `Api` | `https://localhost:7001` |
| `StepUp.AppHost` (Aspire dashboard) | `https://localhost:17201` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database seeding or internet access is needed.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `Api/Controllers/StepUpController.cs` — the endpoints with different requirements
- `Api/Authorization/StepUpHandler.cs` — where the `WWW-Authenticate` header is set
- `Client/StepUpHandler.cs` — the `HttpMessageHandler` that issues the step-up challenge
- `IdentityServerHost/StepUpInteractionResponseGenerator.cs` — handling `acr_values`
- `IdentityServerHost/Pages/Account/Login/Mfa.cshtml` and `RecentAuth.cshtml` — the custom step-up pages

There is also a more detailed walkthrough of the IdentityServer side in
[`IdentityServerHost/README.md`](IdentityServerHost/README.md).
