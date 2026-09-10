# Spaces Sample

This sample demonstrates [Duende Spaces](Documentation.md) — running multiple isolated IdentityServer spaces within a single ASP.NET Core host. Each space has its own users, clients, identity providers, and configuration data, completely isolated from other spaces.

## What This Sample Shows

| Scenario | How It's Demonstrated |
|----------|----------------------|
| **Host-based space resolution** | `space1.dev.localhost:5000` and `space2.dev.localhost:5000` each resolve to their own space |
| **Path-based space resolution** | `localhost:5000/t/space3` resolves to space3 via the `/t` path prefix |
| **Default/fallback space** | `localhost:5000` resolves to the default space |
| **Per-space user isolation** | Each space has its own users; a user in one space cannot sign into another |
| **Per-space client isolation** | Each space has its own client credentials clients |
| **Shared resources across spaces** | An `example-client` and `example-user` are created in every space to show how the same logical entity must be explicitly provisioned per space |
| **Dynamic external identity providers** | Each space has its own OIDC provider (pointing to demo.duendesoftware.com) |
| **Space claim in tokens** | A custom profile service and token request validator add a `space` claim to all issued tokens |
| **Client credentials token request** | The home page requests tokens server-side and displays decoded JWT header, payload, and claims |
| **User Management integration** | Users are created and authenticated via Duende.UserManagement with password authentication |

## Project Structure

```
Spaces/
├── src/Spaces/              # IdentityServer host + Razor Pages UI
│   ├── Program.cs               # App configuration & middleware
│   ├── Seed/
│   │   ├── SeedData.cs          # Seeds spaces, users, clients, IDPs
│   │   └── DemoCredentials.cs   # Centralized demo credentials
│   ├── Services/
│   │   ├── SpaceClaimAugmentationProfileService.cs
│   │   └── AddSpaceNameToClaimsRequestValidator.cs
│   ├── Pages/
│   │   ├── Index.cshtml(.cs)    # Home page with token request
│   │   └── Account/             # Login, logout, external callback
│   └── wwwroot/                 # Minimal CSS/JS (no frameworks)
├── tests/Spaces.PlaywrightTests/  # E2E browser tests
├── aspire/AppHost/              # Aspire orchestration (used by tests)
├── Documentation.md             # Spaces feature documentation
└── Directory.Packages.props     # Central package management
```

## Spaces

| Space | Resolution | URL |
|-------|-----------|-----|
| Default | Fallback (no explicit match) | `https://localhost:5000` |
| space1 | Host-based | `https://space1.dev.localhost:5000` |
| space2 | Host-based | `https://space2.dev.localhost:5000` |
| space3 | Path-based | `https://ignored.localhost:5000/t/space3` |

The sample is configured to fallback if no space matches to the default space. This is analogous 
to when you use a single-spaced IdentityServer with multiple issuers: They all use the same configuration data. 

Space3 is configured to use only path based routing. This means that it completely ignores
the hostname. The reason we chose a different domain name (ignored.localhost) is becuase cookies
are tied to a domain name. In this case, the cookie for the default space is also valid for space 3, which is not desirable. 

Since origin based routing takes precedence, navigating to https://space1.dev.localhost:5000/t/space3 will return 404. This is becuase it's not clear which space to select (space1 via origin or space 3 via path)


## Demo Credentials

All passwords are `Pa$$Word123`.

| Space | Space-Specific User | Shared User |
|-------|-------------------|-------------|
| Default | `default-user` | `example-user` |
| space1 | `space1-user` | `example-user` |
| space2 | `space2-user` | `example-user` |
| space3 | _(none)_ | `example-user` |

### Clients

To demonstrate how to use client credentials, we've also setup several clients. 

Each space has two clients: 
a space specific client (IE: space1-client) and the 'example-client'.  

| Client ID | Spaces | Grant Type | Secret |
|-----------|--------|-----------|--------|
| `default-client` | Default only | client_credentials | `secret` |
| `space1-client` | space1 only | client_credentials | `secret` |
| `space2-client` | space2 only | client_credentials | `secret` |
| `space3-client` | space3 only | client_credentials | `secret` |
| `example-client` | All spaces | client_credentials | `secret` |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running the Sample

```bash
cd src/Spaces
dotnet run
```

The app launches at `https://localhost:5000`.

### Accessing All Spaces

1. **Default space**: Navigate to `https://localhost:5000`
2. **space1**: Navigate to `https://space1.dev.localhost:5000`
3. **space2**: Navigate to `https://space2.dev.localhost:5000`
4. **space3**: Navigate to `https://localhost:5000/t/space3`

### HTTPS Certificate

The sample uses the ASP.NET Core development certificate. Trust it with:

```bash
dotnet dev-certs https --trust
```

## External Login (OIDC)

Each space has a dynamic OIDC identity provider configured pointing to `https://demo.duendesoftware.com`. Click the external provider button on the login page to authenticate via the Duende demo server.

## How It Works

### Space Resolution

Spaces resolves the current space early in the middleware pipeline via `app.UseSpacesResolution()`. This must run before `app.UseIdentityServer()` so that all IdentityServer operations use the correct space context.

- **Host-based**: The request origin is matched against configured `SpaceMatchPattern.Origin` values
- **Path-based**: Requests under `/t/{space-path}` are matched and the path is rewritten for downstream routing

See [Documentation.md](Documentation.md) for full details on resolution, configuration options, and the `ISpaceContextAccessor` API.

### Data Isolation

All data is isolated per space. The seed data demonstrates this by:
- Creating the same `example-user` and `example-client` independently in each space
- Each space has its own user schema, API scopes, and identity providers
- A user authenticated in one space has no session or identity in another

### Space Claim Augmentation

Two custom services add a `space` claim to issued tokens:

1. **`SpaceClaimAugmentationProfileService`** — Wraps `UserManagementProfileService` and adds the space claim for user tokens
2. **`AddSpaceNameToClaimsRequestValidator`** — Adds the space claim during client credentials token requests

This allows downstream APIs to know which space a token was issued from.

## Running Tests

The E2E tests use [Playwright for .NET](https://playwright.dev/dotnet/) and [Aspire Testing](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing):

```bash
# Install Playwright browsers (first time only)
pwsh tests/Spaces.PlaywrightTests/bin/Debug/net10.0/.playwright/package/bin/playwright.ps1 install

# Run tests
dotnet test tests/Spaces.PlaywrightTests
```
## Further Reading

- [Spaces Documentation](Documentation.md) — Full feature documentation including configuration options, space resolution, and management APIs
- [Duende IdentityServer Documentation](https://docs.duendesoftware.com/identityserver/)
- [Duende User Management](https://docs.duendesoftware.com/user-management/)
