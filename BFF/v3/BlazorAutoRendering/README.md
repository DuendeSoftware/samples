# BFF Blazor Auto-Rendering Sample

This sample shows the Blazor "auto rendering" model behind a [Backend-for-Frontend (BFF)](https://docs.duendesoftware.com/identityserver/bff), together with a YARP-proxied remote API.

A single ASP.NET Core host serves the Blazor components with **both** the `InteractiveServer` and `InteractiveWebAssembly` render modes registered, and lets the framework pick per route. The interesting part is `IWeatherClient`: the same abstraction is resolved to a server-side implementation when the component renders on the server, and to an HTTP client that goes through the BFF when it renders in the browser. You get the security of the BFF without giving up interactivity, and without writing the component twice.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`BlazorAutoRendering`** — the BFF host, running on `https://localhost:7035`. It serves the Blazor app, the `/bff/*` management endpoints, a local API endpoint and a proxied remote API endpoint.
- **`BlazorAutoRendering.Client`** — the WebAssembly assembly. It has no launch profile of its own; the host serves it.
- **`BlazorAutoRendering.Api`** — a separate JWT-protected demo API, running on `https://localhost:7001`. This is the one the BFF proxies to.

## How to Run

1. Run the `BlazorAutoRendering` and `BlazorAutoRendering.Api` projects.
1. Browse to `https://localhost:7035` and use the login link.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Visit the Weather and Greet pages, then toggle interactive rendering to compare the two paths.

The sample does not run its own identity server, so you need internet access to reach the demo server.

## What to Look For

- **Render modes** — `Program.cs` registers `AddInteractiveServerRenderMode()` and `AddInteractiveWebAssemblyRenderMode()` and adds the client assembly with `AddAdditionalAssemblies(...)`. `Pages/Home.razor` is `InteractiveServer`, `Pages/Counter.razor` is `InteractiveWebAssembly`, and `Pages/Weather.razor` and `Pages/Greet.razor` use `@rendermode InteractiveAuto` and let the framework choose.
- **The swappable client** — `AddSingleton<IWeatherClient, ServerWeatherClient>()` on the host resolves the interface to an in-process implementation. The client project resolves the *same* interface to a `WeatherClient` that calls the BFF. Because the component only knows `IWeatherClient`, the same code works in both places.
- **The remote API path** — `MapRemoteBffApiEndpoint("/remote-apis/greetings", "https://localhost:7001").RequireAccessToken(TokenType.User)` proxies the greetings API and attaches the user's access token. The client is told where that prefix is with `AddBffBlazorClient(opt => opt.RemoteApiPath = "remote-apis/greetings/")`, so `Pages/Greet.razor` never hard-codes it.
- **Seeing which path ran** — `Pages/Greet.razor` renders `response.RequestMessage?.RequestUri`, so the displayed URL tells you whether the call was served locally or went through the proxy.
- **The local API endpoint** — `WeatherEndpointExtensions.MapWeatherEndpoints()` maps `GET /WeatherForecast` with `.RequireAuthorization().AsBffApiEndpoint()`, which adds the CSRF header check on top of authorization.
- **Two `HttpClient` flavours on the client** — `AddLocalApiHttpClient<WeatherClient>()` talks to the BFF's own endpoints, while `AddRemoteApiHttpClient("greet")` targets the proxied remote API path.
- **`SameSite=Lax` is deliberate here** — `Program.cs` carries a comment explaining that because the identity provider is on `duendesoftware.com` while the BFF is on `localhost`, a `Strict` cookie would not be sent after the redirect back from login. Use `Strict` when your identity provider is on the same site as the BFF.
- **Antiforgery** — `UseBff()` performs the BFF CSRF header check, and `UseAntiforgery()` adds the ASP.NET Core token store that Blazor forms use.
