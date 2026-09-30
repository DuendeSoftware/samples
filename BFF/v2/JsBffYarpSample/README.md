# BFF JavaScript Sample with YARP

This sample is the reverse-proxy flavour of the [JavaScript BFF sample](../v4/JsBffSample). The UI is identical, but instead of handling the ToDo calls itself, the BFF runs a [YARP](https://github.com/dotnet/yarp) reverse proxy that forwards `/todos/**` to a real downstream API, attaching the user's access token on the way out.

This is the pattern to reach for when the API already exists and you do not want to hand-write a controller per endpoint — the BFF becomes a secure gateway instead of a second implementation of your API.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`FrontendHost`** — the BFF, running on `https://localhost:5010`. It serves the static UI (`index.html`, `todo.js`, `session.js`), maps the `/bff/*` management endpoints, and hosts the reverse proxy.
- **`BackendApiHost`** — the downstream JWT-protected ToDo API, running on `https://localhost:5020`. Unlike in the non-YARP sample, this one really is called.

## How to Run

1. Run the `FrontendHost` and `BackendApiHost` projects.
1. Browse to `https://localhost:5010` and use the login button.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Create and delete ToDo items, and watch the proxied calls in the browser's network tab.

## What to Look For

- **The proxy pipeline** — `Program.cs` calls `AddBff()`, then `AddReverseProxy().AddTransforms<AccessTokenTransformProvider>()`, then the sample's own `YarpConfigurator.Configure()`. The provider is the piece that attaches the user's access token to each outgoing request.
- **Routes in code** — `YarpConfigurator.cs` builds the route and cluster configuration in C# with `LoadFromMemory`. The route matches `/todos/{**catch-all}`, points at the `cluster1` cluster, and declares the token requirement with `.WithAccessToken(TokenType.User)`. The cluster's single destination is `https://localhost:5020`.
- **Loading config from memory** — `InMemoryConfigProvider.cs` implements `IProxyConfigProvider` and `IProxyConfig` so the routes can be handed to YARP as an in-memory object rather than a `appsettings.json` section. That is what makes the route able to carry BFF-specific configuration such as the access token requirement.
- **Mapping the proxy** — `app.MapBffReverseProxy()` is the shorthand for `MapReverseProxy().AsBffApiEndpoint()`. The `AsBffApiEndpoint()` part is what enforces the session check and the CSRF header.
- **Going back to a local API** — the sample keeps `ToDoController.cs` and the matching `MapControllers().AsBffApiEndpoint()` call, commented out, so you can switch between the two styles and compare.
- **The CSRF header** — the UI sends `x-csrf: 1` on its requests, which the BFF requires before it will forward anything.
