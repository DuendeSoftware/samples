# BFF OpenAPI Sample

This sample shows a BFF that proxies two downstream APIs *and* rewrites their OpenAPI documents on the fly, so the front end sees a single, browser-facing API surface.

Normally a downstream API's OpenAPI document describes server-to-server calls: it names the API's own `servers` entry and its JWT `securitySchemes`. Neither is useful to a browser talking to a BFF with a cookie. This sample intercepts each `/openapi/*.json` response as it is proxied through YARP and rewrites it — repointing `servers` at the BFF, dropping the security schemes, and prefixing every path with the BFF's local path. It then merges both APIs into one combined document, so Swagger UI and any client generator see the whole BFF surface at once.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`OpenApi.Api1`** — a minimal API exposing `GET /weatherforecastFahrenheit`, running on `https://localhost:7289`. It commits its generated OpenAPI document (`OpenApi.Api1.json`) and is protected by JWT bearer authentication.
- **`OpenApi.Api2`** — the same, exposing `GET /WeatherforecastCelcius`, running on `https://localhost:7297`.
- **`OpenApi.Bff`** — the BFF, running on `https://localhost:7082`. Hosts a static UI and Swagger UI, proxies both APIs, and rewrites and combines their OpenAPI documents. Its OIDC, cookie and frontend settings are read from the `BFF` section of `appsettings.json`.
- **`OpenApi.BffOpenApiDocumentParser`** — a small console app that applies the same document transformation offline, so a pre-transformed document can be produced at build time for tooling or code generation. `OpenApi.Bff` references it as a project so both share one implementation.
- **`OpenApi.DevServer.AppHost`** — the .NET Aspire host that starts everything and wires up service discovery. Its dashboard runs on `https://localhost:17275`.
- **`OpenApi.DevServer.ServiceDefaults`** — the shared Aspire service defaults (OpenTelemetry, health checks, JWT bearer validation) referenced by the other projects.

## How to Run

1. Run the `OpenApi.DevServer.AppHost` project (or use the Aspire CLI with `aspire run`), which starts the two APIs and the BFF for you.
1. Open the Aspire dashboard at `https://localhost:17275` and click through to the BFF.
1. Log in with username `bob` and password `bob` (or `alice` / `alice`) at the [Duende demo identity server](https://demo.duendesoftware.com).
1. Browse to `https://localhost:7082` for the UI, or `https://localhost:7082/swagger` for Swagger UI.

You can also run the projects individually without Aspire, in which case the fixed ports in each `launchSettings.json` apply.

## What to Look For

- **Rewriting the documents** — `OpenApiResponseTransform.cs` is registered as a YARP response transform. When a proxied response is an OpenAPI document it suppresses the original body and pipes it through `OpenApiTransformer.TransformOpenApiDocumentForBff`, which clears `Servers` and `Components.SecuritySchemes` and prefixes every path with the BFF's local path.
- **Combining the documents** — `OpenApiDocumentCombiner.cs` fetches each source document and merges the `Paths` and `Components` into one. Security schemes are intentionally *not* copied, because the browser never presents a bearer token to the BFF. Served from `/swagger/combined/v1.json`.
- **The BFF endpoint mapping** — `app.MapRemoteBffApiEndpoint("/api1", uri).WithAccessToken(RequiredTokenType.UserOrNone)` and the same for `/api2`. Both APIs are optional here so the document endpoints stay reachable.
- **Swagger UI as the browser** — `UseSwaggerUI(...)` registers all three documents, injects an `X-CSRF` request interceptor, and adds `bff-auth-button.js` so you can drive the "try it" buttons from the BFF session.
- **Service discovery** — `AddHttpForwarderWithServiceDiscovery()` lets YARP resolve the logical service names, so the BFF does not need to know the APIs' real addresses.
- **BFF 4.x configuration style** — the BFF reads its OIDC, cookie and frontend settings straight from the `BFF` section of `appsettings.json` via `AddBff().LoadConfiguration(...)`. There is no hand-written configuration POCO in this version.
- **OpenAPI library version** — this version uses `Microsoft.OpenApi` 2.x, so the document is read with `OpenApiDocument.LoadAsync(...)` and written with `SerializeAsJsonAsync(...)` rather than the older `OpenApiStreamReader` API.
