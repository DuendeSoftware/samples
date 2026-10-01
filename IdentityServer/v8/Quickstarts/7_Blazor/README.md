# Blazor WebAssembly with a BFF sample

This sample shows how to protect a **Blazor WebAssembly** application using the
[Backend-for-Frontend (BFF) pattern](https://docs.duendesoftware.com/identityserver/bff), so that
tokens never reach the browser.

### What the sample demonstrates

- `builder.Services.AddBff()` from `Duende.BFF` to set up the BFF services
- `app.UseBff()` — must be placed between `UseAuthentication()` and `UseAuthorization()`
- `app.MapBffManagementEndpoints()`, which exposes the `/bff/login`, `/bff/logout` and `/bff/user` endpoints
  used by the frontend
- `AddOpenIdConnectAccessTokenManagement()` so the BFF caches, refreshes and forwards tokens automatically
- protecting a BFF API endpoint with `.AsBffApiEndpoint()`, which requires a CSRF header from the caller
- `AddBffBlazorClient()` in the Blazor WASM project, providing a typed `BffContext` client
- `AddCascadingAuthenticationState()` so `AuthorizeView` works in Razor components
- a `__Host-` prefixed, `SameSite=Strict` authentication cookie

> **Note:** this sample does **not** run its own IdentityServer. It is a confidential client of the public
> Duende demo server at `https://demo.duendesoftware.com`, so you need internet access.

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project AppHost
   ```

   > Only the BFF is started by the AppHost. The Blazor WASM project is served by the BFF itself
   > (`MapFallbackToFile("index.html")`), so it is not a separate running resource.

   The Aspire dashboard opens at `https://localhost:17236` and will show a single `bff` resource.

1. Open a browser tab to `https://localhost:7256`. You are anonymous; the home page shows an empty claim list.
1. Click **bff/login** in the navigation. This redirects to the demo IdentityServer.
1. Log in as `bob` / `bob` (or `alice` / `alice`) and approve the request.
1. You are returned to the app and the home page now shows the claims. Notice that the nav has gained a
   **bff/logout** link, taken from the `bff:logout_url` claim that `MapBffManagementEndpoints` emits.
1. Click **Weather**. The page calls `GET /api/data` through the typed BFF client, which attaches the
   `X-CSRF: 1` header that `.AsBffApiEndpoint()` requires. The weather rows are returned and rendered.
1. Click **Counter** to see plain client-side Blazor state, with no BFF involved.
1. Click **bff/logout** to clear the cookie and end the session at the demo server.

## Credentials

| Username | Password |
|----------|----------|
| `bob` | `bob` |
| `alice` | `alice` |

These are the users of the public demo IdentityServer at `https://demo.duendesoftware.com`.

## Projects and URLs

| Project | URL |
|---------|-----|
| `BFF` (serves both the BFF and the Blazor WASM app) | `https://localhost:7256` |
| `AppHost` (Aspire dashboard) | `https://localhost:17236` |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- Internet access to `https://demo.duendesoftware.com`

No database or seeding is required — the demo server holds the configuration.

> **Note:** `BlazorQuickstart.slnx` contains only the `BFF` and `BlazorWasm` projects, not `AppHost`.
> If you open the solution in an IDE and press run, you will start the BFF without the Aspire dashboard,
> which is fine — just open `https://localhost:7256`.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `BFF/Program.cs` — the entire BFF setup
- `BFF/weather.json` — the static payload served by the BFF API endpoint
- `BlazorWasm/Program.cs` — `AddBffBlazorClient()`
- `BlazorWasm/Layout/MainLayout.razor` — the `bff/login` and `bff/logout` links
- `BlazorWasm/Pages/Home.razor` — the claims dump
- `BlazorWasm/Pages/Weather.razor` — the call to `GET /api/data`

For a JavaScript SPA using a BFF instead, see quickstart
[`6_JS_with_backend`](../6_JS_with_backend).
