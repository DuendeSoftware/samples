# Multi-Frontend Single Sign-Out Demonstration

## Overview

This sample illustrates coordinated sign-out across multiple frontends that share the same IDP User Session. When a user signs out from one frontend application, they are automatically signed out from all other frontend applications in the same browser session.

## Sample Structure

- **MultiFrontendSSO.IdentityServer** - The identity provider built with Duende IdentityServer
- **MultiFrontendSSO.Client** - A frontend application using the Backend-For-Frontend (BFF) pattern with Duende BFF 4.0 RC2

## Key Features

- **Single Sign-Out**: Sign out from one application triggers sign-out across all applications
- **BFF Security Pattern**: Backend-For-Frontend architecture protects tokens and handles authentication flows server-side

## Prerequisites

- .NET 9.0 SDK
- Duende BFF 4.0 RC2
- Duende IdentityServer

## Running the Sample

1. Start IdentityServer:
   ```
   cd MultiFrontendSSO.IdentityServer
   dotnet run
   ```
2. Start the Client application:
   ```
   cd MultiFrontendSSO.Client
   dotnet run
   ```
3. Navigate to the client application in your browser at https://localhost:5002
4. Sign In to both frontends. 
    - The first frontend signed into would require credentials (`bob:bob`) to be provided. Once signed in, the user has an active session at the IDP.
    - Subsequent signins, should not prompt you for credentials as you have an active session with the identity provider.
5a. Sign out from any of the frontends.
    - When a frontend initiates logout, IdentityServer sends backchannel logout notifications to all other clients that share the same user session by making server-to-server HTTP POST requests to each client's configured BackChannelLogoutUri.
    - Each client's BFF backchannel endpoint receives the notification and clears its local session, effectively signing the user out of all frontends.
5b. Sign out of the IDP at https://localhost:5001
    - Once you completed the sign out at the IDP, refresh your client browser or use the link back to the frontend and notice that you have been signed out of both frontends.
6. Observe that once signed out of 1 frontend, that navigating to the 2nd frontend also indicates that you are not signed in.
