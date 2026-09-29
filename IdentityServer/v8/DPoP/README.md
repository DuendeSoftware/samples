# DPoP (Demonstration of Proof of Possession) sample

This sample shows how to bind access tokens to a client using **DPoP** ([RFC 9449](https://datatracker.ietf.org/doc/html/rfc9449)),
so that a stolen bearer token is useless without the corresponding private key.

### What the sample demonstrates

- a client configured with `RequireDPoP = true`, which makes IdentityServer issue a **DPoP-bound access
  token**: `typ` is `at+jwt` and the token carries a `cnf.jkt` claim with the client's key thumbprint
- generating a DPoP key on the client and handing it to `Duende.AccessTokenManagement`
  (`options.DPoPJsonWebKey`), so a proof is attached automatically to every token request and every API call
- resource server validation with `ConfigureDPoPTokensForScheme`, which checks the proof signature,
  the `jkt` to `cnf.jkt` binding, the `htm`/`htu` method and URI, and proof freshness
- `DPoPProofExpirationMode` — this sample uses the default `IssuedAt` mode; the alternative `Nonce` mode
  has the API issue a `DPoP-Nonce` that the client must echo, at the cost of an extra round trip
- **replay protection** using a keyed `HybridCache` in front of an `IDistributedCache`
- DPoP for both **interactive** (authorization code) and **machine-to-machine** (client credentials) clients
- an API that echoes back the scheme, the raw DPoP proof, the raw access token and the claims, so the
  whole exchange is visible in the browser

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project DPoP.AppHost
   ```

1. Open a browser tab to the web client at `https://localhost:5010`.
1. Click on the **Secure** link in the navigation. You will be redirected to IdentityServer at
   `https://localhost:5001`.
1. Log in with username `alice` and password `alice` (or `bob` / `bob`) and grant consent.
1. You will be redirected back to the `client` application's `/Secure` page, which lists the user's
   claims and the authentication cookie properties. The **Renew Tokens** button forces a fresh
   DPoP-bound access token.
1. Click the **Call API** button. The page shows the JSON returned by the API. Look at:
   - `"scheme": "DPoP"` — the API selected the DPoP scheme, not `Bearer`
   - `proofToken` — a `dpop+jwt` proof signed with `PS256`, containing `jkt`, `htm`, `htu` and `iat`
   - `accessToken` — an `at+jwt` whose `cnf.jkt` matches the proof's `jkt`
1. Click **Logout**. The web client revokes the refresh token on sign-out.
1. At the same time, look at the `clientcredentials` console in the Aspire dashboard. That background
   service calls the API every 5 seconds with a DPoP-bound client credentials token and logs the same
   JSON, demonstrating the machine-to-machine case.

## Seeded users

The IdentityServer is the shared `../IdentityServerHost` host, which uses in-memory test users:

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

The `dpop` client id and secret are `dpop` / `905e4892-7610-44cb-a122-6209b38c882f` if you want to
call the token endpoint by hand.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `IdentityServerHost` (shared, from `../IdentityServerHost`) | `https://localhost:5001` | issues DPoP-bound tokens |
| `Api` | `https://localhost:5005` | validates DPoP proofs |
| `WebClient` | `https://localhost:5010` | interactive client — **open this one** |
| `ClientCredentials` | — | background worker, no URL |
| `DPoP.AppHost` | `https://localhost:17008` | Aspire dashboard |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database, Docker container or external service is required.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `Api/Program.cs` — `ValidTypes = ["at+jwt"]`, `ConfigureDPoPTokensForScheme`, the proof expiration mode and the replay cache
- `Api/IdentityController.cs` — the response that makes DPoP visible
- `WebClient/Program.cs` — creating the RSA DPoP key and `AddUserAccessTokenHttpClient`
- `ClientCredentials/Program.cs` and `ClientCredentials/DPoPClient.cs` — DPoP for client credentials
- `../IdentityServerHost/src/Clients.cs` — the `dpop` client with `RequireDPoP = true`
