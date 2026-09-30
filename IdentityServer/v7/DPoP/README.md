# DPoP (Demonstration of Proof of Possession) sample

[DPoP](https://datatracker.ietf.org/doc/html/rfc9449) (Demonstration of Proof of Possession) binds
an access token to the cryptographic key that requested it. Instead of sending a bearer token that
anyone who obtains it can replay, the client must also send a freshly signed proof over a key it
holds. If the token is stolen, the thief cannot use it — they do not have the private key.

This sample shows DPoP on both halves of the flow: acquiring tokens at the token endpoint, and
presenting them at a protected API.

### What the sample demonstrates

**Token issuance — the `dpop` client** (registered in `IdentityServerHost\src\Clients.cs`)

- `RequireDPoP = true` on the client, which makes IdentityServer reject any token request that does
  not carry a valid DPoP proof
- `AllowedGrantTypes = GrantTypes.CodeAndClientCredentials` — DPoP works with both the interactive
  code flow and the machine-to-machine flow, and this sample exercises both
- a client secret (`905e4892-7610-44cb-a122-6209b38c882f`) in addition to the DPoP key; the two
  authenticate different things

**The interactive client** (`WebClient`)

- `AddOpenIdConnectAccessTokenManagement()` combined with
  `options.DPoPJsonWebKey = DPoPProofKey.Parse(...)` — an RSA-2048 JWK with `Alg = "PS256"`, which
  is the proof key
- code flow with PKCE, and `options.OnSigningOut` calling `RevokeRefreshTokenAsync()` so the refresh
  token is revoked on sign-out
- `AddUserAccessTokenHttpClient("client", ...)` pointing at the API; the proof is attached
  automatically on each request
- a `Secure` page with a **Renew Tokens** button, a `Call API` page, and a `Renew` action that forces
  token renewal (`ForceTokenRenewal = true`)

**The machine-to-machine client** (`ClientCredentials`)

- `AddClientCredentialsTokenManagement().AddClient("dpop", ...)` with `client.DPoPProofKey` set
- `AddClientCredentialsHttpClient("client", ...)` — a `BackgroundService` that calls the API every
  five seconds, so you can watch the DPoP round trip repeatedly without touching the browser

**The API** (`Api`)

- `options.TokenValidationParameters.ValidTypes = ["at+jwt"]`, so only an access token is accepted
- `ConfigureDPoPTokensForScheme("token", ...)`, layering DPoP validation on top of the plain JWT
  bearer scheme
- `opt.ProofTokenExpirationMode = DPoPProofExpirationMode.IssuedAt`, with the alternative `Nonce`
  mode explained in a comment
- a keyed `HybridCache` registered as the proof-token replay cache
  (`ServiceProviderKeys.ProofTokenReplayHybridCache`) and backed by a distributed memory cache —
  this is what stops an attacker re-sending a captured proof
- `options.MapInboundClaims = false` and `ValidateAudience = false`, because the sample uses API
  scopes rather than an API resource

> **Note:** this sample does **not** ship its own IdentityServer project. It reuses the shared
> `IdentityServerHost` at the root of the v7 tree, which is also used by the other samples in this
> version. If you change the `dpop` client in `Clients.cs` while another sample is running, you
> affect them too.

### How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project DPoP.AppHost
   ```

   The Aspire dashboard opens at `https://localhost:17007` and shows four resources:
   `identityserverhost`, `api`, `webclient` and `clientcredentials`.

1. Open a browser tab to the `webclient` at `https://localhost:5010`.
1. Sign in. The IdentityServer login page appears at `https://localhost:5001`; log in with
   `alice` / `alice` (or `bob` / `bob`) and approve the request.
1. You are returned to the WebClient. The home page is anonymous. Use the **Secure** link in the
   navigation to trigger the interactive flow.
1. Click **Call API** in the navigation. The client attaches the access token *and* a DPoP proof to
   `GET https://localhost:5005/identity`, and the API echoes back the token claims.
1. Open the browser dev tools, go to **Network**, and inspect the request headers. Alongside
   `Authorization: DPoP <token>` you will find a `DPoP` header containing the signed proof JWT. This
   is the visible difference from a plain bearer flow, where the header would just be `Bearer`.
1. Go back to **Secure** and click **Renew Tokens**. This forces the access token to be refreshed
   (`ForceTokenRenewal = true`); call the API again and watch the new token and proof in the network
   tab.
1. Watch the `clientcredentials` resource logs in the Aspire dashboard. Every five seconds it obtains
   a DPoP-bound token and calls the API, logging the result. This is the client credentials half of
   the sample running unattended.
1. To see the replay protection work, capture a request from the network tab and replay the exact
   same `Authorization` and `DPoP` headers with `curl`. The API rejects it, because the proof has
   already been seen in the replay cache.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

The `ClientCredentials` client authenticates as a machine, not a user:

| Client id | Client secret | Scope |
|-----------|---------------|-------|
| `dpop` | `905e4892-7610-44cb-a122-6209b38c882f` | `scope1` |

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `WebClient` | `https://localhost:5010` | Interactive client, code flow + DPoP |
| `IdentityServerHost` | `https://localhost:5001` | Shared v7 IdentityServer; registers the `dpop` client |
| `Api` | `https://localhost:5005` | Protected API, DPoP-validated |
| `ClientCredentials` | _(console)_ | Machine-to-machine DPoP client, polls every 5s |
| `DPoP.AppHost` | `https://localhost:17007` | Aspire dashboard |

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

- `..\IdentityServerHost\src\Clients.cs` — the `dpop` client with `RequireDPoP = true`
- `WebClient/Program.cs` — the `DPoPJsonWebKey` and the token management setup
- `WebClient/Controllers/HomeController.cs` — the `Secure`, `CallApi` and `Renew` actions
- `ClientCredentials/Program.cs` — `AddClientCredentialsTokenManagement` with a DPoP key
- `Api/Program.cs` — `ValidTypes`, `ConfigureDPoPTokensForScheme` and the replay cache
- `WebClient/TokenResponseExtensions.cs` — pretty-printing the API response
