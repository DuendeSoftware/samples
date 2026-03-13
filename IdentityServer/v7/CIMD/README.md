# CIMD Sample

This sample demonstrates how to use **Client ID Metadata Documents (CIMD)** to secure an MCP server with OAuth, as an alternative to Dynamic Client Registration (DCR).

With CIMD, the MCP client (VS Code) uses a URL as its `client_id`. The authorization server fetches a metadata document from that URL to learn about the client — no pre-registration or DCR endpoint is needed.

## Components

### CIMD.IdentityServer

A Duende IdentityServer configured to support CIMD. Key differences from a standard IdentityServer setup:

- **`CimdClientStore`** — A custom `IClientStore` that, when it receives an unknown `client_id`, treats it as a URL, fetches the metadata document, validates it per the CIMD spec (SSRF checks, auth method restrictions, client_id matching), and dynamically creates a `Client`.
- **`ICimdPolicy` / `McpCimdPolicy`** — A policy interface that controls which domains are allowed and can modify or restrict document fields. The sample policy allows all domains and merges default scopes (`openid`, `profile`, `mcp`) into documents that don't declare scopes.
- **Discovery** — Advertises `"client_id_metadata_document_supported": true` so clients know CIMD is available.
- **No DCR** — Unlike the McpDemo sample, there is no `AddIdentityServerConfiguration`, `AddInMemoryClientConfigurationStore`, or `MapDynamicClientRegistration`.
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

## Why CIMD Instead of DCR?

| | DCR | CIMD |
|---|---|---|
| **Client registration** | Client POSTs metadata to a registration endpoint | Client hosts metadata at a URL; server fetches it |
| **Server endpoint** | Requires a `/register` endpoint | No registration endpoint needed |
| **Client identity** | Server assigns a `client_id` | The metadata URL *is* the `client_id` |
| **Trust model** | Server trusts any client that can reach the registration endpoint | Server fetches and validates metadata, can apply domain policies |
