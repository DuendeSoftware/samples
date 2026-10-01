# BFF Token Exchange Sample

This sample shows [OAuth 2.0 Token Exchange](https://www.rfc-editor.org/rfc/rfc8693) in a BFF. Before calling the downstream API's `/api/impersonation` endpoint, the BFF takes the signed-in user's access token and exchanges it at the identity provider's token endpoint for a *different* one.

The BFF authenticates as its own confidential client (`spa` / `secret`) and passes the user's token as the `subject_token`, with `subject_token_type=urn:ietf:params:oauth:token-type:access_token` and `grant_type=urn:ietf:params:oauth:grant-type:token-exchange`. The custom grant validator in the identity provider swaps `alice` for `bob` (and vice versa), so the API receives an access token for the *other* user while the browser still holds a session for the original one. This is the classic delegation / impersonation flow.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`TokenExchange.IdentityServer`** — a full Duende IdentityServer host, running on `https://localhost:5001`. This sample ships its own identity provider (rather than using the public demo server) so the token exchange grant can be demonstrated. `TokenExchangeGrantValidator.cs` implements the `IExtensionGrantValidator` that performs the user swap.
- **`TokenExchange.Bff`** — the BFF, running on `https://localhost:6001`. Hosts a static UI and hosts both the plain and the impersonating API endpoints.
- **`TokenExchange.Api`** — the downstream API, running on `https://localhost:7001`. A JWT-protected resource API whose echo endpoint shows which user the presented token actually represents.

## How to Run

1. Run the `TokenExchange.IdentityServer`, `TokenExchange.Bff` and `TokenExchange.Api` projects.
1. Browse to `https://localhost:6001` and log in.
1. Log in with username `alice` and password `alice` (or `bob` / `bob`).
1. Call the user-token endpoint, then the impersonation endpoint, and compare the `sub` claim that comes back each time.

## What to Look For

- **The exchange on the BFF side** — `ImpersonationAccessTokenRetriever.cs` derives from `DefaultAccessTokenRetriever`, gets the current user's access token via the base implementation, and then calls `HttpClient.RequestTokenExchangeTokenAsync(...)` with a `TokenExchangeTokenRequest` pointed at the identity provider's `/connect/token` endpoint. The result is returned as a `BearerTokenResult` so the proxy can use it like any other token.
- **Wiring the retriever to an endpoint** — `/api/user-token` is a plain remote endpoint requiring a user access token. `/api/impersonation` adds `.WithAccessTokenRetriever<ImpersonationAccessTokenRetriever>()`, which is the only difference between the two.
- **The exchange on the identity provider side** — `TokenExchangeGrantValidator.cs` implements `IExtensionGrantValidator` with `GrantType => OidcConstants.GrantTypes.TokenExchange`. It validates the incoming `subject_token` with `ITokenValidator.ValidateAccessTokenAsync`, requires `subject_token_type` to be an access token, and returns a grant result whose subject is the *other* user, with `authenticationMethod: "swap-alice-and-bob"`.
- **The token exchange grant is not built in** — registering a custom extension grant validator via `isBuilder.AddExtensionGrantValidator<TokenExchangeGrantValidator>()` is all that is needed; Duende IdentityServer does not ship a token exchange grant out of the box.
- **Test users** — `TokenExchange.IdentityServer/Pages/TestUsers.cs` holds the in-memory users `alice` / `alice` and `bob` / `bob`. The `spa` / `secret` client is allowed the `authorization_code`, `client_credentials` and `token-exchange` grants.
