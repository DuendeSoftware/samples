# CIMD Sample

This sample demonstrates how to use **Client ID Metadata Documents (CIMD)** to secure an MCP server with OAuth.

With CIMD, the MCP client (in this demo, VS Code) uses a URL as its `client_id`. The authorization server fetches a metadata document from that URL to learn about the client without needing pre-registration or DCR.

## Components

### CIMD.IdentityServer

A Duende IdentityServer configured to support CIMD. Key differences from a standard IdentityServer setup:

- **`CimdClientStore`** — A custom `IClientStore` that, when it receives an unknown `client_id`, treats it as a URL, fetches the metadata document, validates it per the CIMD spec (SSRF checks, auth method restrictions, client_id matching), and dynamically creates a `Client`.
- **`ICimdPolicy` / `McpCimdPolicy`** — A policy interface that controls which domains are allowed and can modify or restrict document fields. The sample policy allows all domains and merges default scopes (`openid`, `profile`, `mcp`) into documents that don't declare scopes.
- **Discovery** — Advertises `"client_id_metadata_document_supported": true` so clients know CIMD is available.
- **AppAuth Redirect URI Validation** — VS Code is a native app, so it performs OAuth using the [RFC 8252](https://datatracker.ietf.org/doc/html/rfc8252) (OAuth 2.0 for Native Apps) flow. It starts a temporary HTTP listener on `http://127.0.0.1` with a random port to receive the authorization callback. IdentityServer's `AddAppAuthRedirectUriValidator()` enables this by allowing loopback redirect URIs with any port.

Runs on `https://localhost:5101`.

### CIMD.McpServer

An MCP server using Streamable HTTP transport, protected by OAuth. It validates JWT bearer tokens issued by the IdentityServer and exposes weather tools (alerts and forecasts from the NWS API).

The MCP resource metadata advertises:
- **Resource**: `https://localhost:7241`
- **Authorization Server**: `https://localhost:5101/`
- **Scopes**: `mcp`

Runs on `https://localhost:7241`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [mkcert](https://github.com/FiloSottile/mkcert) — for generating locally-trusted HTTPS certificates
- [VS Code](https://code.visualstudio.com/) with GitHub Copilot (as the MCP client)

## Certificate Setup (mkcert)

VS Code's MCP client uses Node.js internally, which does not trust .NET's development certificates (`dotnet dev-certs`). We use `mkcert` to create certificates from a locally-trusted CA that Node.js will accept.

### Install mkcert

- **macOS**: `brew install mkcert`
- **Windows**: `choco install mkcert`
- **Linux**: See [mkcert installation](https://github.com/FiloSottile/mkcert#installation)

### Generate Certificates

Run once to install the local CA (this adds mkcert's root CA to your OS trust store):

```bash
mkcert -install
```

Then generate certificates for each project:

```bash
cd CIMD.IdentityServer
mkcert localhost

cd ../CIMD.McpServer
mkcert localhost
```

This creates `localhost.pem` and `localhost-key.pem` in each project directory. These files are gitignored.

## Running the Sample

Start both servers (in separate terminals):

```bash
# Terminal 1
dotnet run --project CIMD.IdentityServer

# Terminal 2
dotnet run --project CIMD.McpServer
```

## Using with VS Code

1. Open `.vscode/mcp.json` — VS Code will show a **Start** button above the `cimd-demo` server entry
2. Click **Start** to connect to the MCP server at `https://localhost:7241/`
3. The MCP server's resource metadata points VS Code to the authorization server at `https://localhost:5101/`
4. VS Code uses a CIMD URL as its `client_id` — IdentityServer fetches the metadata document from that URL to learn about the client
5. VS Code opens a browser for you to log in (use one of the [test users](https://github.com/DuendeSoftware/Samples/blob/main/IdentityServer/v7/CIMD/CIMD.IdentityServer/Pages/TestUsers.cs))
6. After login, VS Code receives tokens and can call MCP tools

The weather tools (`GetAlerts`, `GetForecast`) should appear in Copilot chat once the server is connected.

## Adding CIMD to Your Own IdentityServer

To add CIMD support to an existing IdentityServer project, you need to copy the custom CIMD types and wire up the discovery and HTTP client configuration.

### 1. Copy the CIMD Types

Copy these files from `CIMD.IdentityServer/` into your project:

| File | Purpose |
|------|---------|
| `CimdDocument.cs` | Model representing a CIMD metadata document (reuses the RFC 7591 JSON schema from `Duende.IdentityModel`) |
| `CimdDocumentFetcher.cs` | Fetches CIMD documents and JWKS from remote URLs with size limits and security checks |
| `CimdDocumentValidator.cs` | Validates that `client_id` matches the document URL and that no shared secrets are used |
| `CimdClientStore.cs` | Decorating `IClientStore` that resolves CIMD URLs into `Client` objects, with caching |
| `CimdClientBuilder.cs` | Converts a `CimdDocument` into a Duende `Client` model |
| `CimdRequestContext.cs` | Bundles the fetched document with its URI and HTTP headers for policy inspection |
| `ICimdPolicy.cs` | Policy interface for domain allow/deny and post-fetch document validation |
| `McpCimdPolicy.cs` | Sample policy — allows all domains and merges default scopes. **Replace this with your own policy.** |
| `SsrfGuard.cs` | SSRF protection that blocks requests to private/reserved IP ranges per the CIMD spec |

### 2. Add NuGet Dependencies

Ensure your project references:

```xml
<PackageReference Include="Duende.IdentityModel" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="10.4.0" />
```

(`Duende.IdentityServer` provides `ValidatingClientStore<T>` and `IClientStore` — you likely already have it.)

### 3. Wire Up Services

In your `HostingExtensions.cs` (or wherever you configure IdentityServer), add:

**Discovery** — advertise CIMD support so clients know it's available:

```csharp
options.Discovery.CustomEntries.Add("client_id_metadata_document_supported", true);
```

**AppAuth redirect URIs** — if your CIMD clients are native apps (like VS Code) that use loopback redirects:

```csharp
isBuilder.AddAppAuthRedirectUriValidator();
```

**CIMD client store** — stack CIMD resolution on top of your existing client store:

```csharp
isBuilder.AddCimdClientStore<InMemoryClientStore>(); // or your IClientStore implementation
```

The `AddCimdClientStore<T>` extension method (included in the copied files) registers the `HybridCache`, policy, SSRF guard, document fetcher, and a named HTTP client with redirect-following disabled per the CIMD spec.

### 4. Customize the Policy

Replace `McpCimdPolicy` with your own `ICimdPolicy` implementation to control:

- **`CheckDomainAsync`** — which domains are allowed to host CIMD documents (the sample allows all)
- **`ValidateDocumentAsync`** — additional validation or modification of fetched documents (the sample merges default scopes)

## Why CIMD Instead of DCR?

| | DCR | CIMD |
|---|---|---|
| **Client registration** | Client POSTs metadata to a registration endpoint | Client hosts metadata at a URL; server fetches it |
| **Server endpoint** | Requires a `/register` endpoint | No registration endpoint needed |
| **Client identity** | Server assigns a `client_id` | The metadata URL *is* the `client_id` |
| **Trust model** | Server trusts any client that can reach the registration endpoint | Server fetches and validates metadata, can apply domain policies |
