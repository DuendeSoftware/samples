# Blazor BFF Quickstart

This is the smallest complete BFF + Blazor app in the repository — a good starting point to copy from.

There is one protected endpoint and nothing else: no YARP, no remote APIs, no separate API host. If you want to understand the moving parts before adding complexity, start here and then look at the [Blazor auto-rendering sample](../BlazorAutoRendering) or the [Blazor WebAssembly sample](../BlazorWasm).

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`BlazorBffApp`** — the BFF host, running on `https://localhost:7007`. It serves the Blazor app, the `/bff/*` management endpoints and the local API endpoint.
- **`BlazorBffApp.Client`** — the WebAssembly assembly. It has no launch profile of its own; the host serves it.

## How to Run

1. Run the `BlazorBffApp` project.
1. Browse to `https://localhost:7007` and use the login link.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Visit the Weather page, which calls the protected endpoint, then sign out.

The sample does not run its own identity server, so you need internet access to reach the demo server.

## What to Look For

- **The whole BFF setup** — `Program.cs` is a single file you can read top to bottom: `AddBff().AddServerSideSessions().AddBlazorServer()`, an authentication block setting up the cookie and OIDC handlers, then `UseBff()` between `UseAuthentication()` and `UseAuthorization()`.
- **`AddServerSideSessions()`** — keeps session state on the server rather than inside the encrypted cookie, which is what lets the BFF hold access tokens across requests.
- **`AddBlazorServer()`** — the BFF-specific registration that lets Blazor components participate in the BFF session.
- **The cookie configuration** — `__Host-blazor` with `SameSite=Lax`. The sample includes a comment explaining that `Lax` is needed here only because the identity provider is on `duendesoftware.com` while the BFF is on `localhost`; use `Strict` when both are on the same site. The `__Host-` prefix requires HTTPS and forbids an explicit cookie `Domain`.
- **The local API endpoint** — `app.MapGet("/WeatherForecast", ...)` returns the forecasts, and the client calls it with `AddLocalApiHttpClient<WeatherHttpClient>()`, which attaches the CSRF header the BFF requires.
- **The swapped client** — the host registers `IWeatherClient` to an inline `ServerWeatherClient` that returns random data without any HTTP call, while the WebAssembly project registers it to a `WeatherHttpClient` that goes through the BFF. Same interface, two implementations, and the Blazor component does not care which one it got.
- **The client setup** — `BlazorBffApp.Client/Program.cs` is five lines: `AddBffBlazorClient()` for the authentication state provider that polls `/bff/user`, `AddLocalApiHttpClient<WeatherHttpClient>()`, the `IWeatherClient` registration and `AddCascadingAuthenticationState()` so `AuthorizeView` works in components.
- **Antiforgery** — `UseBff()` performs the BFF CSRF header check, and `UseAntiforgery()` adds the ASP.NET Core token store that Blazor forms use.
