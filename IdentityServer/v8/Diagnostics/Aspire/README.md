# Aspire diagnostics sample

This sample shows how to get **logs, metrics and traces** out of Duende IdentityServer using .NET Aspire
and OpenTelemetry, and how to wire the surrounding services together with Aspire's service discovery,
resilience and health check defaults.

### What the sample demonstrates

- `builder.AddServiceDefaults()` in every service, which configures:
  - **OpenTelemetry logs** via `builder.Logging.AddOpenTelemetry(...)`
  - **OpenTelemetry metrics** from `AddRuntimeInstrumentation()`, `AddBuiltInMeters()` and
    `AddMeter("Duende.IdentityServer", "Duende.IdentityServer.Experimental", "IdentityServer")`
  - **OpenTelemetry traces** from `AddAspNetCoreInstrumentation()`, `AddHttpClientInstrumentation()`,
    `AddGrpcClientInstrumentation()` and `AddSource("Duende.IdentityServer")`
  - OTLP exporters, which Aspire enables automatically by setting `OTEL_EXPORTER_OTLP_ENDPOINT`
  - health checks at `/health` and `/alive`
  - **service discovery** (`AddServiceDiscovery()`) and the **standard resilience handler** for every `HttpClient`
- custom application metrics emitted from the IdentityServer UI pages, using a meter named after this
  host: `tokenservice.user_login`, `tokenservice.user_logout`, `tokenservice.consent`, `tokenservice.grants_revoked`
- `Duende.AccessTokenManagement` combined with a deliberately short `AccessTokenLifetime` so that tokens
  renew constantly and you can watch a steady stream of token requests in the dashboard
- Aspire service discovery in action: the web frontend resolves the API by its logical name
  (`https://apiservice`) rather than a hard-coded port

## How to Run

1. Run the Aspire project:

   ```bash
   dotnet run --project Aspire.AppHost
   ```

1. The Aspire dashboard opens automatically at `https://localhost:15264`.
1. On the **Resources** page, click the endpoint link for the `webfrontend` resource to open the app
   (or navigate to `https://localhost:5014`).
1. Log in as `alice` / `alice` and approve consent. The home page shows a table of weather data
   fetched from the API service.
1. Now explore the dashboard while clicking around the app:
   - **Traces** — filter by the `identityserver` service to see spans from the `Duende.IdentityServer`
     activity source across the authorize and token endpoints, plus the outgoing calls from the web
     frontend to the API service.
   - **Metrics** — look for counters under `Duende.IdentityServer.*` and `IdentityServer.tokenservice.*`.
     The `user_login` and `consent` counters increment as you sign in and grant consent.
   - **Logs** — structured logs from all three services. Because the client access token lifetime is
     70 seconds, you will see a token request roughly every 10 seconds while the session is active.
   - **Resources** — each service exposes `/health` and `/alive`.

## Seeded users

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL | Exposed in dashboard |
|---------|-----|----------------------|
| `IdentityServer` | `https://localhost:5001` | yes |
| `Aspire.Web` | `https://localhost:5014` | yes |
| `Aspire.ApiService` | `https://localhost:5325` | no (internal only) |
| `Aspire.AppHost` (Aspire dashboard) | `https://localhost:15264` | — |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database, Docker container or external service is required.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `Aspire.ServiceDefaults/Extensions.cs` — all the OpenTelemetry, health check, service discovery and resilience wiring
- `Aspire.AppHost/Program.cs` — the service topology
- `IdentityServer/Pages/Telemetry.cs` — the custom `tokenservice.*` counters
- `IdentityServer/HostingExtensions.cs` — the IdentityServer setup and the short token lifetime
- `Aspire.Web/Program.cs` and `Aspire.Web/WeatherApiClient.cs` — service discovery and the user access token handler
- `Aspire.ApiService/Program.cs` — the `weather` scope fallback policy

For a console-only version of the same idea, with no Aspire and no dashboard, see the
[OpenTelemetry sample](../Otel/README.md).
