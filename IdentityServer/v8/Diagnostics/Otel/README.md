# OpenTelemetry console tracing sample

This is the minimal version of the [Aspire diagnostics sample](../Aspire/README.md): it shows how to turn on
OpenTelemetry **tracing** inside IdentityServer with no collector, no Aspire and no dashboard. The spans are
printed straight to the console.

### What the sample demonstrates

- `builder.Services.AddOpenTelemetry().WithTracing(...)` with a **console exporter**, so spans show up in
  the terminal
- subscribing to IdentityServer's **fine-grained tracing sources** via
  `IdentityServerConstants.Tracing.Basic`, `.Cache`, `.Services`, `.Stores` and `.Validation`
- `AddHttpClientInstrumentation()`, `AddAspNetCoreInstrumentation()` and `AddSqlClientInstrumentation()`
- setting the OpenTelemetry `resource` service name with `ConfigureResource(r => r.AddService(...))`
- an OTLP exporter you can switch on by uncommenting a single line, if you would rather send spans to a collector
- structured logging through Serilog, including `UseSerilogRequestLogging()`

## How to Run

1. Run the project:

   ```bash
   dotnet run --project src
   ```

1. The browser opens at `https://localhost:5001`. **Watch the terminal** — that is where the traces appear.
1. Log in as `alice` / `alice` (or `bob` / `bob`) using the **Login** link.
1. Navigate around the app. Each request produces a Serilog request log line followed by a stream of
   OpenTelemetry spans showing the activity, trace and span ids and the originating source
   (`Duende.IdentityServer.*`, ASP.NET Core, or `System.Net.Http`).
1. To generate traces on the token endpoint as well, request a token from another terminal:

   ```bash
   curl --fail-with-body -X POST "https://localhost:5001/connect/token" \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=client_credentials&client_id=m2m.client&client_secret=511536EF-F270-4058-80CA-1C89C192F69A&scope=scope1"
   ```

1. `https://localhost:5001/diagnostics` shows the claims in the current authentication cookie
   (restricted to requests from the local machine).

## Seeded users

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

## Projects and URLs

| Project | URL |
|---------|-----|
| `src` | `https://localhost:5001` |

There is no AppHost for this sample — it is a single project.

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `src/HostingExtensions.cs` — the whole OpenTelemetry setup
- `src/Program.cs` — the Serilog bootstrap logger and `UseSerilog`
- `src/Config.cs` — the clients and scopes, including the `m2m.client` used in the `curl` example above

If you want metrics, logs and a dashboard rather than console traces only, use the
[Aspire sample](../Aspire/README.md) instead.
