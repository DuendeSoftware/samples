# SAML 2.0 Basic Sample

This sample demonstrates SP-initiated SAML 2.0 single sign-on using Duende IdentityServer as the Identity Provider (IdP) and Sustainsys.Saml2 as the Service Provider (SP). It covers the full authentication lifecycle including login and single logout (SLO).

## Architecture

The solution contains three projects orchestrated by .NET Aspire:

- **IdentityServerHost** — Duende IdentityServer configured as a SAML 2.0 IdP using `.AddSaml()` with an in-memory service provider registration. It uses the standard IdentityServer UI template with test users (alice/bob, password: `alice`/`bob`).
- **ServiceProvider** — An ASP.NET Core Razor Pages application using Sustainsys.Saml2.AspNetCore2 for SAML authentication. It displays user claims after successful authentication and supports SAML single logout.
- **Basic.AppHost** — The Aspire orchestrator that generates an ephemeral self-signed certificate at startup for the SP to sign logout requests. The public key is passed to the IdP for signature validation, and the full PFX is passed to the SP for signing — all via environment variables with no manual certificate setup required.

## Running the Sample

Run the AppHost project to start both the IdentityServer IdP and the Service Provider. The Aspire dashboard will show both services. Navigate to the Service Provider (https://localhost:5002) and click "Secure" to initiate a SAML login. You will be redirected to IdentityServer to authenticate, then returned to the SP with your claims displayed.

## Key Concepts

The SP signing certificate is generated in-memory by the AppHost at startup, avoiding any manual certificate provisioning. The IdP is configured to require signed authentication requests when the SP certificate is available (`RequireSignedAuthnRequests` is set conditionally), so the sample also works if run without the AppHost for simpler experimentation. Single logout is fully wired: the SP signs its logout request, and the IdP validates the signature using the SP's public key before processing the logout.
