# BFF Blazor WebAssembly Sample

This sample shows a standalone Blazor WebAssembly application protected by a [Backend-for-Frontend (BFF)](https://docs.duendesoftware.com/identityserver/bff), so that no tokens ever reach the browser.

An ordinary ASP.NET Core application authenticates the user with OIDC and then serves the Blazor WebAssembly app with `UseBlazorFrameworkFiles()` and `MapFallbackToFile("index.html")`. The WASM app talks to the BFF's own endpoints and never sees an access token.

This version deliberately does the client-side plumbing **by hand**, which makes it useful for understanding what the official Blazor packages do for you in later versions.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`BlazorWasm.Server`** — the BFF host, running on `https://localhost:7189`. It handles the OIDC login, maps the `/bff/*` management endpoints, and serves the WebAssembly app itself.
- **`BlazorWasm.Client`** — the Blazor WebAssembly application.
- **`BlazorWasm.Shared`** — a small class library holding the `WeatherForecast` model that both projects use.

Because the host serves the WebAssembly app, **the only URL you need is `https://localhost:7189`**.

## How to Run

1. Run the `BlazorWasm.Server` project.
1. Browse to `https://localhost:7189` and use the login link in the navigation.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Visit the Fetch Data page, which calls a protected BFF endpoint, then sign out.

This sample does not run its own identity server, so you need internet access to reach the demo server.

## What to Look For

- **The custom authentication state provider** — `BlazorWasm.Client/BFF/BffAuthenticationStateProvider.cs` is an `AuthenticationStateProvider` that reads the session by calling `bff/user?slide=false`, deserialises the claim list, and builds a `ClaimsIdentity`. It caches the result for 60 seconds and runs a background timer to notice when the session disappears. This is roughly what `AddBffBlazorClient()` gives you in later versions.
- **The custom CSRF handler** — `BlazorWasm.Client/BFF/AntiforgeryHandler.cs` is an eleven-line `DelegatingHandler` that adds the `X-CSRF: 1` header to every outgoing request. Registering it with `AddHttpClient("backend", ...).AddHttpMessageHandler<AntiforgeryHandler>()` is how the client satisfies the CSRF requirement of a BFF API endpoint.
- **Server-side sessions** — the `__Host-blazor` cookie is `SameSite=Strict`, so the BFF does not need a server-side session store; the token is held in the encrypted cookie.
- **Protecting an endpoint** — `BlazorWasm.Server/Program.cs` maps its controllers with `MapControllers().RequireAuthorization().AsBffApiEndpoint()`. The `AsBffApiEndpoint()` call is what adds the CSRF header check on top of ordinary authorization.
- **Middleware order** — `app.UseBff()` sits between `UseAuthentication()` and `UseAuthorization()`.
- **The CSRF header itself** — this version relies on the header check performed by `UseBff()`; it does not additionally call ASP.NET Core's `UseAntiforgery()` middleware.
