# BFF JavaScript Sample

This sample shows the most basic shape of a BFF: an ASP.NET Core application that serves a plain JavaScript UI and handles the authentication and token handling for the browser, so no tokens ever reach JavaScript.

The UI is served from `wwwroot` and talks to same-origin endpoints only. Log in and out happen through the BFF's management endpoints, and the session is read back from `/bff/user`.

Please take a look [here](https://docs.duendesoftware.com/identityserver/samples) to learn about the structure of our samples and how to run them.

## Sample Structure

- **`FrontendHost`** — the BFF, running on `https://localhost:5010`. It serves the static UI (`index.html`, `todo.js`, `session.js`), maps the `/bff/*` management endpoints, and hosts the ToDo API in the same process.
- **`BackendApiHost`** — a separate JWT-protected ToDo API, running on `https://localhost:5020`. It is not used in the default configuration, but it is there so you can compare the two styles.

## How to Run

1. Run the `FrontendHost` and `BackendApiHost` projects.
1. Browse to `https://localhost:5010` and use the login button.
1. Log in at the [Duende demo identity server](https://demo.duendesoftware.com) with username `bob` and password `bob` (or `alice` / `alice`).
1. Create, update and delete ToDo items, then reload the page to see the session restored from the cookie.

## What to Look For

- **Login and logout** — the UI simply links to `/bff/login?returnUrl=/` and `/bff/logout?returnUrl=/`. All of the OAuth/OIDC flow happens in the BFF.
- **Reading the session** — `wwwroot/session.js` fetches `/bff/user` and renders the user's name and claims. JavaScript never sees an access token; it only learns *who* is logged in.
- **The CSRF header** — every call from the UI sends an `x-csrf: 1` header. This is what lets the BFF reject cross-site request forgery on its API endpoints.
- **The local API** — `FrontendHost/ToDoController.cs` is an ordinary MVC controller marked with `.RequireAuthorization().AsBffApiEndpoint()`. That marks it as a BFF endpoint, which requires both a valid session and a valid CSRF header.
- **Switching to a remote API** — `Program.cs` contains a commented-out `MapRemoteBffApiEndpoint("/todos", "https://localhost:5020/todos")` showing the alternative: keep the same UI and let the BFF proxy the calls to a real downstream API instead of handling them locally.
- **BFF 4.x configuration style** — the OIDC and cookie setup is part of the BFF builder: `.ConfigureOpenIdConnect(o => { ... })` and `.ConfigureCookies(o => { ... })` replace the separate `AddCookie` / `AddOpenIdConnect` calls, and the default schemes come from the `BffAuthenticationSchemes` constants.
- **Data protection** — `AddDataProtection().SetApplicationName("BFF")` gives the BFF a stable application name so its data-protected session state survives restarts and works across replicas. See the [data protection docs](https://docs.duendesoftware.com/general/data-protection).
