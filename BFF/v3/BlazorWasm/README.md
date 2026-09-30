# BFF Blazor WebAssembly Sample

This sample shows a standalone Blazor WebAssembly application protected by a [Backend-for-Frontend (BFF)](https://docs.duendesoftware.com/identityserver/bff), so that no tokens ever reach the browser.

An ordinary ASP.NET Core application authenticates the user with OIDC and then serves the Blazor WebAssembly app with `UseBlazorFrameworkFiles()` and `MapFallbackToFile("index.html")`. The WASM app talks to the BFF's own endpoints and never sees an access token.

Compared with the [BFF v2 version](../v2/BlazorWasm) of this sample, the hand-written authentication state provider and CSRF handler are replaced by the official `Duende.BFF.Blazor.Client` package, and the BFF gains server-side sessions.

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

- **The Blazor server-side helpers** — `Program.cs` in the host calls `AddBff().AddServerSideSessions().AddBlazorServer()`. `AddServerSideSessions()` keeps session state on the server rather than in the cookie, and `AddBlazorServer()` wires up the pieces Blazor needs.
- **The whole client setup, in four lines** — `Program.cs` in `BlazorWasm.Client` is now just `AddBffBlazorClient().AddCascadingAuthenticationState()` plus `AddLocalApiHttpClient("backend")`. The `AddBffBlazorClient()` call provides the authentication state provider that polls the `/bff/user` endpoint, and `AddLocalApiHttpClient` provides an `HttpClient` that already attaches the required CSRF header and talks to the BFF's local endpoints.
- **`CascadingAuthenticationState`** — this is what makes `AuthorizeView` and `AuthorizeRouteView` work in Razor components.
- **Antiforgery** — the host now calls `app.UseAntiforgery()` after `UseAuthorization()`, in addition to the CSRF header check that `UseBff()` performs.
- **The session cookie** — still `__Host-blazor` with `SameSite=Strict`. The `__Host-` prefix requires HTTPS and forbids setting an explicit `Domain`.
- **Protecting an endpoint** — `MapControllers().RequireAuthorization().AsBffApiEndpoint()` combines a valid session with a valid CSRF header.
