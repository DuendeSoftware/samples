# JavaScript client sample

This sample shows a **pure JavaScript SPA** talking directly to IdentityServer from the browser, with
tokens held in the browser. It is the standalone version of the older JavaScript quickstart.

### What the sample demonstrates

- an OpenID Connect client built with the [`oidc-client`](https://github.com/IdentityModel/oidc-client-js)
  library (`Oidc.UserManager`), which is vendored in `wwwroot/lib/oidc-client.js`
- the **authorization code flow with PKCE** via `signinRedirect()` and `signinRedirectCallback()`
- tokens stored in the browser's `sessionStorage` rather than in a server-side session
- a deliberate, hand-rolled `XMLHttpRequest` that attaches `Authorization: Bearer <access_token>` to the API
  call, so you can see exactly how the raw token is sent
- RP-initiated logout via `signoutRedirect()`

> **Note:** this is a legacy sample. The project targets `net6.0` and uses the older `Startup` class
> pattern, and it is a *client only* — it contains neither an IdentityServer nor an API. It is not part
> of the repository solution and will need a newer target framework to build on a current SDK.
>
> For a maintained equivalent that runs end to end, use the
> [`6_JS_without_backend`](../6_JS_without_backend) quickstart. If you are looking for the modern
> approach with tokens kept server-side, use [`6_JS_with_backend`](../6_JS_with_backend) instead.

## How to Run

Because this project is a client only, you must start the IdentityServer and the API it expects first.
The matching host is the [`6_JS_without_backend`](../6_JS_without_backend) quickstart, which serves:

| Service | URL | Registered by |
|---------|-----|---------------|
| IdentityServer | `https://localhost:5001` | `6_JS_without_backend` — client `js`, redirect `https://localhost:5003/callback.html` |
| API (`/identity`) | `https://localhost:6001` | `6_JS_without_backend` |
| This SPA | `https://localhost:5003` | this project |

1. Start the `6_JS_without_backend` quickstart (it has its own Aspire AppHost) so the IdentityServer and
   the API are running.
1. Start this project:

   ```bash
   dotnet run --project JavaScriptClient.csproj
   ```

1. The browser opens at `https://localhost:5003/index.html`. The results pane logs **User not logged in**.
1. Click **Login**. You are redirected to `https://localhost:5001/connect/authorize` with
   `response_type=code`, a PKCE `code_challenge`, and `redirect_uri=https://localhost:5003/callback.html`.
1. Log in at the IdentityServer as `alice` / `alice` (or `bob` / `bob`) and consent.
1. The IdentityServer redirects to `https://localhost:5003/callback.html?code=...&state=...`, which
   calls `signinRedirectCallback()` and returns to `index.html`. The results pane now shows
   **User logged in** and the pretty-printed user profile. The access token lives in `sessionStorage`.
1. Click **Call API**. The sample issues a `GET https://localhost:6001/identity` with the bearer token and
   shows the JSON identity payload.
   - A `401` means the token's audience or scope is wrong.
   - A CORS error in the browser dev tools means the API's allowed-origins list is missing
     `https://localhost:5003`.
1. Click **Logout**. `signoutRedirect()` goes to the IdentityServer's end-session endpoint, the
   `User signed out of IdentityServer` event fires, and you land back on `index.html` logged out.

## Seeded users

These are the in-memory test users of the `6_JS_without_backend` IdentityServer:

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Prerequisites

- .NET SDK able to build a `net6.0` web project
- A running IdentityServer at `https://localhost:5001` with the `js` client registered, and an API at
  `https://localhost:6001` — in this repository, the `6_JS_without_backend` quickstart provides both
- Trusted HTTPS development certificates for ports 5001, 6001 and 5003: `dotnet dev-certs https --trust`
- CORS configured on the API for the origin `https://localhost:5003`

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `wwwroot/app.js` — the OIDC configuration and every call
- `wwwroot/index.html` — the login, call API and logout buttons
- `wwwroot/callback.html` — reads `?code` and `?state`, then completes the redirect
- `wwwroot/lib/oidc-client.js` — the vendored `oidc-client` library
