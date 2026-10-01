# Client Credentials sample

This sample shows how to use the OAuth 2.0 **client credentials** grant. This is a machine-to-machine
flow: there is no browser and no user, so the client authenticates with a client id and secret and
receives an access token it can use to call a protected API.

### What the sample demonstrates

- a client registered with `AllowedGrantTypes = GrantTypes.ClientCredentials` and a hashed
  `ClientSecrets` entry (`new Secret("secret".Sha256())`)
- a protected API scope (`api1`) that the client is allowed to request
- client authentication at the token endpoint using HTTP Basic-style client id/secret credentials
- API authorization via a policy that requires the `scope` claim to contain `api1`
- JWT bearer validation against the IdentityServer's discovery metadata
- using `Duende.IdentityModel` (`GetDiscoveryDocumentAsync`, `RequestClientCredentialsTokenAsync`,
  `SetBearerToken`) to run the whole exchange from a console app

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project src/AppHost
   ```

   > `AppHost` is deliberately **not** a member of `Quickstart.sln`, so `--project` is required.

   The Aspire dashboard opens at `https://localhost:17236` and lists three resources: `identityserver`,
   `api` and `client`.

1. Click the `client` resource in the dashboard. It is a console application, so its output appears in
   the dashboard logs. It runs once and then exits.

1. To watch it again, either restart the `client` resource or run it directly:

   ```bash
   dotnet run --project src/Client
   ```

   It prints the access token it received, then calls the API and prints the identity claims returned
   by the `/identity` endpoint.

1. To see a failure instead, stop the `identityserver` resource and run `src/Client` again — discovery
   will fail and the client prints the error.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServer` | `https://localhost:5001` | Issues tokens |
| `Api` | `https://localhost:6001` | Protected API, `GET /identity` |
| `Client` | _(console)_ | Requests a token and calls the API |
| `AppHost` | `https://localhost:17236` | Aspire dashboard |

## Credentials

There are no users in this sample — the client credentials grant has no interactive user.

| Client id | Client secret | Scope |
|-----------|---------------|-------|
| `client` | `secret` | `api1` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- The [Aspire](https://learn.microsoft.com/dotnet/aspire/) workload (`dotnet workload install aspire`)

No database or seeding step is required — the configuration is in-memory.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `src/IdentityServer/Config.cs` — the `client` registration
- `src/Client/Program.cs` — the entire token request, end to end
- `src/Api/Program.cs` — the `ApiScope` authorization policy

For an interactive equivalent, see quickstart [`2_InteractiveAspNetCore`](../2_InteractiveAspNetCore).
