# BFF Split Hosts Sample

This sample shows what changes when the SPA and the BFF are **not** on the same origin. Instead of one application serving both the UI and the BFF endpoints, the UI is served by a bare static-file host on `https://localhost:5011` and the BFF runs separately on `https://localhost:5010`.

The browser therefore makes *cross-origin* requests to the BFF, with `credentials: "include"`. That is the interesting part of this sample: CORS with credentials, cookie behaviour across origins, and the return-URL validation you need so that login and logout can only ever return to the SPA.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`FrontendHost`** — a static-file server only, running on `https://localhost:5011`. Its `Program.cs` is eleven lines and it references no BFF packages at all. It serves the same `index.html`, `todo.js` and `session.js` used in the other JavaScript samples.
- **`BackendHost`** — this is the BFF, running on `https://localhost:5010`. Despite the name, it is not a downstream service — it hosts the `/bff/*` management endpoints, the ToDo API, and the CORS policy. The browser is what calls it.
- **`BackendApiHost`** — a separate JWT-protected ToDo API, running on `https://localhost:5020`. It is not used in the default configuration.

## How to Run

1. Run the `FrontendHost`, `BackendHost` and `BackendApiHost` projects.
1. Browse to `https://localhost:5011` — this is the UI, and it is the URL you start from.
1. Use the login button, which redirects to the BFF on `:5010` and back again.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Create and delete ToDo items, and watch the cross-origin calls in the network tab.

## What to Look For

- **The SPA points at the BFF explicitly** — `wwwroot/todo.js` builds its requests against `https://localhost:5010/todos` and sets `credentials: "include"`, and `wwwroot/session.js` reads `https://localhost:5010/bff/user`. The `returnUrl` in the login and logout links is `https://localhost:5011`, not `/`.
- **CORS is locked down to the SPA** — `Program.cs` in `BackendHost` registers a default policy allowing only the origin `https://localhost:5011`, only the `x-csrf` and `content-type` headers, only the `DELETE` method, and crucially `AllowCredentials()`. The app then calls `app.UseCors()`.
- **The return-URL validator** — `FrontendHostReturnUrlValidator.cs` implements `IReturnUrlValidator` and only accepts a return URL on host `localhost`, port `5011`. Without this, the BFF's `returnUrl` parameter would be an open redirect.
- **Why the cookie still works** — the session cookie is `__Host-bff` with `SameSite = Strict`. The `__Host-` prefix requires HTTPS and no explicit `Domain`, and `Strict` cookies are still sent cross-origin here because `:5010` and `:5011` are the *same site* (same host, different ports). Change the ports to different hostnames and the sample would need `SameSite = None` plus `Secure`.
- **No BFF code in the frontend** — `FrontendHost` is deliberately package-free. The split costs you nothing in terms of what the BFF can do; it only adds the CORS and return-URL configuration on the BFF side.
