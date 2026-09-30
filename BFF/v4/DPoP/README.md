# BFF DPoP Sample

This sample shows a BFF that requests *DPoP-bound* access tokens (Demonstration of Proof of Possession, [RFC 9449](https://www.rfc-editor.org/rfc/rfc9449)) and calls a downstream API with them.

Instead of a bearer token that anyone can replay, DPoP binds the access token to a key that only the BFF holds. For every API call the BFF generates a short-lived *DPoP proof* JWT that covers the HTTP method and target URI, and the API checks that proof against the request it actually received. A stolen access token on its own is therefore useless without the corresponding private key.

The sample shows the four access-token flavours side by side — anonymous, user, client credentials, and user-or-client — both through the BFF's own endpoint helpers and through raw YARP routes.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`DPoP.Api`** — the downstream API, running on `https://localhost:6001`. It validates the access token *and* the DPoP proof on every request, and echoes back the path, message and headers. It ships a self-contained DPoP validation implementation under `DPoP.Api/DPoP/`, so you can see exactly what a resource server has to do.
- **`DPoP.Bff`** — the BFF, running on `https://localhost:5002`. It hosts a small static UI and proxies the API calls on the browser's behalf.

## How to Run

1. Run the `DPoP.Bff` and `DPoP.Api` projects.
1. Browse to `https://localhost:5002` and log in.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Try the different endpoints and watch the requests and responses in the browser's network tab.

## What to Look For

- **Enabling DPoP in the BFF** — `Program.cs` in `DPoP.Bff` creates a 2048-bit RSA key, converts it to a JWK, sets `Alg` to `PS256`, and wraps it in a `DPoPProofKey`, which is then assigned to `options.DPoPJsonWebKey` inside `AddBff(options => { ... })`. Setting this key is all it takes to switch the BFF over to DPoP.
- **BFF 4.x configuration style** — the OIDC and cookie setup is now part of the BFF builder: `.ConfigureOpenIdConnect(o => { ... })` and `.ConfigureCookies(o => { ... })` replace the separate `AddCookie` / `AddOpenIdConnect` calls, and the default schemes come from the `BffAuthenticationSchemes` constants (`BffCookie`, `BffOpenIdConnect`).
- **Data protection** — `AddDataProtection().SetApplicationName("BFF")` gives the BFF a stable application name so its data-protected state (including the DPoP key material) survives restarts and works across replicas. See the [data protection docs](https://docs.duendesoftware.com/general/data-protection).
- **Token types** — the BFF exposes `/api/anonymous`, `/api/user-token`, `/api/client-token` and `/api/user-or-client-token`, each guarded by the matching `WithAccessToken(RequiredTokenType.X)` requirement, plus `/api/optional-user-token` (`RequiredTokenType.UserOrNone`) for the "send a token if we have one" case.
- **YARP routes** — `/yarp/anonymous`, `/yarp/user-token`, `/yarp/client-token` and `/yarp/user-or-client-token` show the same calls driven purely by the YARP configuration in `YarpConfigurator.cs`.
- **Proof validation on the API** — `DPoP.Api/DPoP/DPoPProofValidator.cs` validates the proof's `htm`, `htu` and `ath` claims against the incoming request and access token, and enforces a one-second proof validity window.
- **DPoP nonces** — the API supports server-issued nonces. `DPoPOptions.ValidateNonce` defaults to `false`; when enabled the API responds with a `DPoP-Nonce` header and rejects the request until the client retries with that nonce.
- **No forwarded headers on the API** — the API deliberately does *not* use forwarded-headers middleware, because it would change the `htu` that the DPoP proof is validated against.
