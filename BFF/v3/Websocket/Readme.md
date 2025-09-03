# WebSocket BFF Sample

This sample demonstrates how WebSockets can be proxied through a Backend for Frontend (BFF) pattern using Duende BFF. The BFF exchanges authentication cookies for access tokens to secure WebSocket connections to backend services.

## Getting Started

To run this sample, start the AppHost project:

```bash
cd Websocket.AppHost
dotnet run
```

This will launch the .NET Aspire dashboard and orchestrate all the required services:
- **BFF** (https://localhost:7140) - The main entry point
- **GraphQL Server** (http://localhost:5095) - Backend service with WebSocket subscriptions
- **React Frontend** (http://localhost:5173) - Client application

Once running, navigate to the BFF URL (https://localhost:7140) to access the application.

## Project Structure

### Websocket.AppHost
A .NET Aspire AppHost project that orchestrates the entire solution. It configures and launches:
- The BFF service
- The GraphQL server
- The React frontend development server

### Websocket.Bff
The Backend for Frontend service built with Duende BFF that:
- Handles OIDC authentication with short-lived access tokens (75 seconds)
- Proxies WebSocket connections to the GraphQL server
- Exchanges authentication cookies for JWT access tokens
- Routes traffic between the frontend and backend services

Key features:
- Uses `interactive.confidential.short` client for demonstration of token expiration
- Proxies `/graphql` endpoint to the GraphQL server with user access tokens
- Serves the React frontend from `/`

### Websocket.GraphQLServer
A GraphQL server with WebSocket subscription support that:
- Validates JWT tokens from the BFF
- Provides GraphQL queries, mutations, and subscriptions
- Includes middleware for WebSocket authentication
- Demonstrates real-time data streaming over WebSockets

### Websocket.React
A React frontend application that:
- Connects to GraphQL subscriptions via WebSockets through the BFF
- Handles authentication state management
- Demonstrates automatic reconnection when tokens expire
- Provides a UI for testing GraphQL operations and subscriptions

### Websocket.Console
A console application for testing WebSocket connections outside of the browser environment.

## Goals and Architecture

This sample demonstrates several key concepts:

### WebSocket Proxying Through BFF
The BFF acts as a secure proxy for WebSocket connections, enabling:
- Centralized authentication and authorization
- Token management and refresh
- Secure communication between frontend and backend services

### Token Exchange and Lifecycle Management
The BFF exchanges authentication cookies for short-lived access tokens (75 seconds) that are used to authenticate WebSocket connections. This demonstrates:

1. **Cookie-to-Token Exchange**: The BFF converts browser cookies into JWT access tokens
2. **Short-lived Tokens**: Access tokens expire after 75 seconds to demonstrate token refresh scenarios
3. **Automatic Reconnection**: When tokens expire, the WebSocket connection is terminated
4. **Seamless Recovery**: The browser automatically attempts to reconnect, and the BFF issues a new token using the refresh token

### Connection Resilience
When an access token expires:
1. The GraphQL server detects the expired token and closes the WebSocket connection
2. The React client detects the connection loss
3. The client automatically attempts to reconnect
4. The BFF uses the refresh token to obtain a new access token
5. The WebSocket connection is re-established with the new token
6. Subscriptions resume seamlessly

This pattern ensures that users experience minimal disruption even with very short-lived access tokens, while maintaining strong security through regular token rotation.

## Authentication Flow

1. User authenticates with the BFF using OIDC
2. BFF stores authentication cookies and refresh tokens
3. Client initiates WebSocket connection through the BFF
4. BFF exchanges the authentication cookie for an access token
5. Access token is used to authenticate the WebSocket connection to the GraphQL server
6. When the token expires (after 75 seconds), the connection is closed
7. Client automatically reconnects, and the process repeats with a fresh token

This demonstrates how modern applications can maintain secure, long-lived connections while using short-lived tokens for enhanced security.