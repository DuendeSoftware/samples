# BFF Angular Sample

This sample shows an [Angular](https://angular.dev/) single-page application hosted by a BFF. The BFF is the only thing the browser talks to: it serves the Angular app, runs the OpenID Connect flow, and holds the tokens, so nothing sensitive ever lands in JavaScript.

Compared to [BFF v2](../v2/Angular), the Angular app itself has moved to server-side rendering, and the client code is written with standalone components and signals instead of an `NgModule` and RxJS streams. None of that changes the BFF side, which makes this a good sample for seeing what SSR does and does not mean for the BFF pattern.

There is also a separate JWT-protected ToDo API in the solution. It is not used in the default configuration — the BFF hosts the ToDo endpoints itself — but the sample ships it so you can switch to the reverse-proxy style with a one-line change.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`Angular.Bff`** — the BFF, running on `https://localhost:6001`. It serves the Angular app from the build output, maps the `/bff/*` management endpoints, and hosts the ToDo API in the same process.
- **`angular.ssr`** — the Angular SSR app, an `.esproj` project so it can take part in the .NET solution and the VS launch profiles. Its dev server runs on `https://localhost:4200`.
- **`Angular.Api`** — a separate JWT-protected ToDo API on `https://localhost:7001`. Not called by default.

## How to Run

1. Run the `Angular.Bff` project. This starts the Angular dev server through the SPA proxy and the BFF together.
1. Browse to `https://localhost:6001` and use the login button.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Create and delete ToDo items, open **Show User Session** to see the claims the BFF returns, and then log out.

## What to Look For

- **SSR, and what it does to the BFF pattern** — the app is built with `"outputMode": "server"` and an Express entry point in `src/server.ts`. The ToDo page is still `RenderMode.Client` and the user-session page is `RenderMode.Server` (see `app.routes.server.ts`), because the ToDo list is fetched from the BFF and has to happen in the browser after the session cookie is sent.
- **The authentication service is signal-based** — `src/app/Services/auth.service.ts` wraps the single `bff/user` request in `toSignal` and derives `isAuthenticated`, `username` and `logoutUrl` with `computed()`. The request itself is still cached with `shareReplay(1)`, so it happens once. The v2 version of this file exposes the same information as RxJS observables.
- **The CSRF header is a functional interceptor** — `src/app/Interceptors/csrf-header.interceptor.ts` is an `HttpInterceptorFn` registered with `provideHttpClient(withFetch(), withInterceptors([csrfHeaderInterceptor]))`. It sets `X-CSRF: 1` and `withCredentials: true` on every request.
- **The SPA proxy** — `Angular.Bff.csproj` sets `SpaRoot`, `SpaProxyLaunchCommand` (`npm start`) and `SpaProxyServerUrl` (`https://localhost:4200`). During development the browser talks to the Angular dev server, and the dev server forwards `/bff`, `/signin-oidc`, `/signout-callback-oidc` and `/todos` on to the BFF (see `proxy.conf.js`), which keeps everything same-origin from the browser's point of view.
- **The local API** — `Program.cs` maps `/todos` with `MapGroup("/todos").ToDoGroup().RequireAuthorization().AsBffApiEndpoint()`. The `AsBffApiEndpoint()` part is what requires both a valid session and a valid CSRF header.
- **Switching to the remote API** — `Program.cs` also contains a commented-out `MapRemoteBffApiEndpoint("/todos", "https://localhost:7001/todos").RequireAccessToken(TokenType.User)`. Comment the local group out and this one in, and the same Angular code now talks to `Angular.Api` through the YARP proxy with the user's access token attached.
- **Client-side routing** — `app.MapFallbackToFile("/index.html")` makes the deep links work when the BFF serves the built app rather than the dev server.
