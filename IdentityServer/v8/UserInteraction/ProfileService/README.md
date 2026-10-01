# Profile service sample

This sample shows how to control the **claims that end up in your tokens** by implementing a custom
`IProfileService`.

### What the sample demonstrates

- registering a custom profile service with `AddProfileService<CustomProfileService>()`
- `GetProfileDataAsync` — the hook that decides what is emitted into the id_token and access token
- `IsActiveAsync` — the hook that decides whether a session is still valid
- adding claims from the **session cookie** (`context.Subject.Claims`) and from a **user store**
  (`_users.FindBySubjectId(...)`)
- the `context.Caller` property, which tells you whether IdentityServer is building the token for the
  client (`ClaimsProviderAccessToken`) or for the userinfo endpoint (`ClaimsProviderUserInfo`)
- the `context.Application.Identifier` property, which lets you emit different claims per client

`CustomProfileService.cs` is heavily commented and shows four separate techniques:

1. emit only the claims the client actually asked for, either from the session or from the user store
2. always emit a `picture` claim, regardless of the requested claim types
3. emit a `tenant` claim when the calling client is `client1`
4. emit a `foo` claim when the caller is `ClaimsProviderAccessToken`

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project ProfileService.AppHost
   ```

1. In the Aspire dashboard, click the endpoint link for the `client` resource to open
   `https://localhost:44300`, or navigate there directly.
1. You will be redirected to the login page at `https://localhost:5001/account/login`. Log in as
   `alice` / `alice`.
1. You are returned to the client's `Home/Secure` page, which lists the user claims and the tokens from
   the authentication cookie.
1. Click **Call API**. The client calls the API at `https://localhost:5002/identity` and displays the JSON.
   Notice the `foo: bar` claim — that is technique 4, and it appears in the **access token** but not in
   the id_token.
1. Click **Refresh token** to perform a refresh token grant and re-issue the session.
1. You can also inspect what was issued directly at `https://localhost:5001/diagnostics` (the access token
   is shown there for a locally signed request).

## Seeded users

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL |
|---------|-----|
| `IdentityServerHost` | `https://localhost:5001` |
| `Client` | `https://localhost:44300` — **open this one** |
| `SimpleApi` (shared, from `../../Apis/SimpleApi`) | `https://localhost:5002` |
| `ProfileService.AppHost` (Aspire dashboard) | `https://localhost:17047` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database seeding is needed — all configuration is in memory.

> **Note on the `tenant` claim:** technique 3 only fires when `context.Application.Identifier == "client1"`,
> but the only client registered in this sample is `interactive.mvc.sample`. To see the `tenant` claim you
> need to rename that client to `client1` or add a client with that id.

> **Note on the Google button:** the login page offers "Sign-in with Google", but the baked-in client id
> has no secret, so the external flow will not complete. Local login is the path to use.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `IdentityServerHost/CustomProfileService.cs` — the entire sample
- `IdentityServerHost/Program.cs` — the `AddProfileService<CustomProfileService>()` registration
- `IdentityServerHost/Clients.cs` — the `interactive.mvc.sample` client
- `Client/Controllers/HomeController.cs` — the **Call API** and **Refresh token** actions
