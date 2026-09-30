# BFF Blazor WebAssembly Sample

This sample shows a standalone Blazor WebAssembly application protected by a [Backend-for-Frontend (BFF)](https://docs.duendesoftware.com/identityserver/bff), so that no tokens ever reach the browser.

An ordinary ASP.NET Core application authenticates the user with OIDC and then serves the Blazor WebAssembly app with `MapStaticAssets()` and `MapFallbackToFile("index.html")`. The WASM app talks to the BFF's own endpoints and never sees an access token. This version also shows how the BFF can proxy a remote API with YARP, adding the user's access token on the way out.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`BFF`** — the BFF host, running on `https://localhost:7256`. It handles the OIDC login, maps the `/bff/*` management endpoints, serves a local API endpoint and a remote API endpoint, and serves the WebAssembly app itself.
- **`BlazorWasm`** — the Blazor WebAssembly application.

Because the host serves the WebAssembly app, **the only URL you need is `https://localhost:7256`**. The `BlazorWasm` project also has its own launch profile on `https://localhost:7206`, which is only used when running the WASM app standalone against the dev server.

Note that this sample no longer has a `Shared` project, and the host project is named `BFF` rather than `Server`. The solution is the newer `BlazorSample.slnx` format.

## How to Run

1. Run the `BFF` project.
1. Browse to `https://localhost:7256` and use the login link in the navigation.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Visit the Weather page, which calls both the local and the remote API endpoint, then sign out.

This sample does not run its own identity server, so you need internet access to reach the demo server. The remote API is served by the demo server as well.

## What to Look For

- **Two kinds of protected endpoint** — `/api/data` is a local endpoint that reads a static `BFF/weather.json` and returns it, guarded by `.RequireAuthorization().AsBffApiEndpoint()`. `/remoteapi` is a YARP endpoint that proxies to `https://demo.duendesoftware.com/api` and attaches the user's access token with `.WithAccessToken()`. The Weather and Claims pages call one of each, so you can compare them.
- **BFF 4.x configuration style** — the OIDC and cookie setup is part of the BFF builder: `.ConfigureOpenIdConnect(o => { ... })` and `.ConfigureCookies(o => { ... })` replace the separate `AddCookie` / `AddOpenIdConnect` calls. `options.ClaimActions.MapAll()` is used so the home page can show every claim the demo server returns.
- **Data protection** — `AddDataProtection().SetApplicationName("BFF")` gives the BFF a stable application name so its data-protected state survives restarts and works across replicas. See the [data protection docs](https://docs.duendesoftware.com/general/data-protection).
- **The client `HttpClient` is registered by hand here** — unlike the other Blazor samples, `BlazorWasm/Program.cs` creates a plain `HttpClient` with `X-CSRF` added to its default request headers, rather than calling `AddLocalApiHttpClient`. `AddBffBlazorClient()` still provides the authentication state provider.
- **Authorization is enforced server-side only** — no page in this sample carries `[Authorize]`, and `App.razor` uses a plain `RouteView` rather than an `AuthorizeRouteView`. The protection lives on the BFF's endpoints, so a page may render while its data calls return a challenge. Compare with the [BlazorAutoRendering sample](../v4/BlazorAutoRendering) for the client-side pattern.
- **The session cookie** — `__Host-blazor` with `SameSite=Strict`.
