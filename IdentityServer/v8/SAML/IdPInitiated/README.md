# SAML 2.0 IdP-Initiated SSO Sample

This sample demonstrates IdP-initiated SAML 2.0 single sign-on using Duende IdentityServer as the Identity Provider (IdP) with a "My Apps" portal dashboard. Users authenticate at the IdP and launch Service Provider applications directly from the dashboard without the SP sending an AuthnRequest first. SP-initiated login is also supported.

## Architecture

The solution contains three projects orchestrated by .NET Aspire:

- **IdentityServerHost** — Duende IdentityServer configured as a SAML 2.0 IdP with `AllowIdpInitiated = true` on each registered Service Provider. It includes a custom "My Apps" Razor Page that uses `IIdpInitiatedSsoService` to generate and send SAML assertions to the selected SP when the user clicks a tile.
- **ServiceProvider** — A single ASP.NET Core Razor Pages application using Sustainsys.Saml2.AspNetCore2, configured via environment variables (entity ID, base URL, display name, signing certificate). The Aspire AppHost launches two instances of this project — "HR Portal" and "Expense Tracker" — each with its own identity and port.
- **IdPInitiated.AppHost** — The Aspire orchestrator that generates ephemeral self-signed certificates (one per SP instance), passes the public keys to the IdP for signature validation, and launches both SP instances with their respective configuration.

## Running the Sample

Run the AppHost project to start the IdentityServer IdP and both Service Provider instances. Navigate to the IdP (https://localhost:5001), log in with a test user (alice/bob, password: `alice`/`bob`), then visit the "My Apps" page. Click "Launch" on any application tile to perform IdP-initiated SSO — you'll be sent directly to the SP already authenticated. You can also navigate to either SP directly and click "Secure" to trigger SP-initiated login.

## Key Concepts

IdP-initiated SSO allows the Identity Provider to send a SAML assertion to a Service Provider without first receiving an AuthnRequest. This is the standard pattern for enterprise portal dashboards where users browse a catalog of available applications. On the IdP side, each SP must have `AllowIdpInitiated = true`, and the portal page injects `IIdpInitiatedSsoService` to generate the SAML response programmatically. On the SP side, Sustainsys.Saml2 must be configured with `AllowUnsolicitedAuthnResponse = true` on the identity provider to accept responses that don't correlate to an outbound request.
