# BFF Vue Sample

This sample shows a [Vue](https://vuejs.org/) single-page application hosted by a BFF project. The SPA is developed with [Vite](https://vite.dev/), but in production it is served as static files from the BFF itself, so everything ends up same-origin and no tokens ever reach JavaScript.

The BFF runs the authorization code flow against the [Duende demo identity server](https://demo.duendesoftware.com), keeps the tokens server-side, and exposes the ToDo API to the browser through its own BFF endpoints. A second, standalone JWT-protected API is included so you can compare hosting the API in the BFF process against proxying to a real downstream service.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`Vue.Bff`** — the BFF, running on `https://localhost:6001`. It serves the SPA, maps the `/bff/*` management endpoints, and hosts the ToDo API in the same process.
- **`Vue.Api`** — a separate JWT-protected ToDo API, running on `https://localhost:7001`. It is not used in the default configuration, but it is there so you can compare the two styles.
- **`vue.client`** — the Vite dev server, running on `https://localhost:4200`. It is a build-time dependency of `Vue.Bff` and is only used during development.

## How to Run

1. Run the `Vue.Bff` and `Vue.Api` projects.
1. Browse to `https://localhost:6001` and use the login button.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Create, update and delete ToDo items, then reload the page to see the session restored from the cookie.

Running `Vue.Bff` starts the Vite dev server through the ASP.NET Core SPA proxy, so you get hot module replacement while developing. The Vite server proxies `/bff/*`, `/todos`, `/signin-oidc` and `/signout-callback-oidc` back to the BFF, which is why the app can use same-origin relative URLs in development and in production alike.

## What to Look For

- **Login, logout and the session** — `vue.client/src/auth.js` keeps the whole story in three reactive refs and three small functions. `checkAuthStatus()` calls `/bff/user` and stores the returned claims, `login()` redirects to `/bff/login`, and `logout()` navigates to the `bff:logout_url` claim value that the BFF hands back in `/bff/user`. JavaScript never sees an access token; it only learns *who* is logged in.
- **One helper for authenticated calls** — `vue.client/src/utils/api.js` exports a single `fetchApi(url, options)` wrapper that adds `credentials: 'include'` and the `X-CSRF: 1` header, and normalizes errors and `204 No Content` responses. Every view goes through it, so the CSRF requirement is never forgotten.
- **The CSRF header** — the `X-CSRF: 1` header is what lets the BFF reject cross-site request forgery on its API endpoints. Omitting it makes the BFF answer `400`.
- **The local API** — `Vue.Bff/ToDoEndpointGroup.cs` holds a small in-memory ToDo store, and `Program.cs` maps it with `.RequireAuthorization().AsBffApiEndpoint()`. That marks it as a BFF endpoint, which requires both a valid session and a valid CSRF header.
- **Switching to a remote API** — `Program.cs` contains a commented-out `MapRemoteBffApiEndpoint("/todos", "https://localhost:7001/todos").RequireAccessToken(Duende.Bff.TokenType.User)` showing the alternative: keep the same UI and let the BFF proxy the calls to a real downstream API instead of handling them locally. The UI does not change at all.
- **Validating tokens in the API** — `Vue.Api` is a plain JWT bearer resource server. It checks the `api` audience and guards the ToDo endpoints with two policies: `ApiCaller` (the token carries the `api` scope) and `InteractiveUser` (the token carries a `sub`, so a real user is behind it). It also calls `WithOpenApi()`, so you can browse the endpoint shape at `/openapi/v1.json`.
- **Client-side routing** — `vue.client/src/router/index.js` uses `createWebHistory` and lazy-loads the ToDo and user-session views. Its `beforeEnter` guards redirect to `/` when nobody is logged in, but that is only a UI convenience — the actual protection is the `RequireAuthorization()` on the BFF endpoints.
- **Explicit authentication schemes** — in BFF 3.x the cookie and OIDC handlers are registered by hand and the default schemes are named `cookie` / `oidc`:
  ```csharp
  builder.Services.AddAuthentication(options =>
  {
      options.DefaultScheme = "cookie";
      options.DefaultChallengeScheme = "oidc";
      options.DefaultSignOutScheme = "oidc";
  }).AddCookie("cookie", options => { /* ... */ })
    .AddOpenIdConnect("oidc", options => { /* ... */ });
  ```
  BFF 4.x folds this into the BFF builder via `.ConfigureOpenIdConnect(...)` and `.ConfigureCookies(...)`, and adds the data-protection setup. See the [BFF v4 sample](../v4/Vue) for that style.
