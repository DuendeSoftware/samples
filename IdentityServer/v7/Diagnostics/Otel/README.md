# Diagnostics with OpenTelemetry sample

This is the minimal version of the diagnostics story: a single Duende IdentityServer project wired
up to OpenTelemetry with a **console exporter**, so you can see the traces IdentityServer emits
without needing a collector, a dashboard or any infrastructure.

If you want metrics, logs, service discovery and a UI to explore them, use the sibling
[`Aspire`](../Aspire) sample instead.

### What the sample demonstrates

- `AddOpenTelemetry()` with `AddService("IdentityServerHost.Sample")` to name the resource
- subscribing to all five built-in IdentityServer trace sources via
  `IdentityServerConstants.Tracing`:
  - `Basic` — request and response lifecycle
  - `Cache` — client and resource cache hits and misses
  - `Services` — the services IdentityServer calls (profile, claims, keys)
  - `Stores` — configuration and operational store access
  - `Validation` — token, authorize and introspection request validation
- `AddConsoleExporter()` for tracing, so spans are printed rather than shipped anywhere
- `AddHttpClientInstrumentation()`, `AddAspNetCoreInstrumentation()` and
  `AddSqlClientInstrumentation()` to correlate IdentityServer spans with the rest of the request
- `options.Events.RaiseErrorEvents`, `RaiseInformationEvents`, `RaiseFailureEvents` and
  `RaiseSuccessEvents` all set to `true`, so the built-in IdentityServer events are raised
- `app.UseSerilogRequestLogging()` for request logging alongside the traces
- an external identity provider, so the login page offers "Sign-in with
  demo.duendesoftware.com" in addition to local login

> **Note:** the client registrations in `Config.cs` use redirect URIs on `https://localhost:44300`,
> but this project runs on `https://localhost:5001` and hosts no client application. That mismatch
> only matters if you try to use a registered client against this host; for tracing the sign-in flow
> it is harmless.

> **Note:** this sample configures **tracing only**. There is no `.AddMeter(...)` call and no custom
> metrics — the `tokenservice.*` counters live in the [`Aspire`](../Aspire) sample.

### How to Run

1. Run the project:

   ```bash
   dotnet run --project src
   ```

   > There is no Aspire AppHost in this sample. `src` is a standalone web project.

   The application listens on `https://localhost:5001`.

1. Open a browser tab to `https://localhost:5001`. The home page is anonymous; you will be
   redirected to the login page.
1. Log in with `alice` / `alice` (or `bob` / `bob`).
1. Watch the console. Every request now produces `Activity` spans written to stdout, including:
   - the ASP.NET Core request span
   - IdentityServer's own spans for the login and authorization handling
   - the `Validation` spans for the token requests
1. Exercise the flows and watch new spans appear:
   - click **Grants** to list the grants for the current user
   - click **Diagnostics** to open the developer diagnostics page, then sign in as `alice` and issue
     a token to generate a full request trace
1. The console output is the raw OpenTelemetry format. To explore the same data in a UI, switch the
   console exporter for `tracing.AddOtlpExporter()` (the line is already present, commented out, in
   `src/HostingExtensions.cs`) and point it at a collector.

## Credentials

| Username | Password |
|----------|----------|
| `alice` | `alice` |
| `bob` | `bob` |

Test users are registered in-memory with `AddTestUsers`.

## Projects and URLs

| Project | URL | Role |
|---------|-----|------|
| `src` | `https://localhost:5001` | The IdentityServer; writes traces to the console |

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

No database, seeding step or Aspire workload is required.

## More Information

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the
structure of our samples and how to run them.

The interesting parts of this sample are:

- `src/HostingExtensions.cs` — the whole OpenTelemetry setup
- `src/Config.cs` — the in-memory client, resource and scope configuration
- `src/Pages/TestUsers.cs` — the `alice` and `bob` users

For the full Aspire-based version with metrics and a dashboard, see [`Aspire`](../Aspire).
