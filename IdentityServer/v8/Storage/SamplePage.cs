// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;

namespace Storage;

internal static class SamplePage
{
    internal const string StateCookie = "storage_sample_state";
    internal const string VerifierCookie = "storage_sample_verifier";

    internal static string Create(string baseUrl)
    {
        var encodedBaseUrl = WebUtility.HtmlEncode(baseUrl);

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Duende Storage IdentityServer sample</title>
              <style>
                body { font-family: system-ui, sans-serif; line-height: 1.5; margin: 2rem auto; max-width: 60rem; padding: 0 1rem; }
                code, pre { background: #f3f4f6; border-radius: .25rem; }
                code { padding: .1rem .25rem; }
                pre { overflow-x: auto; padding: 1rem; white-space: pre-wrap; }
              </style>
            </head>
            <body>
              <h1>Storage-backed IdentityServer</h1>
              <p>
                This host stores IdentityServer configuration and operational data in an in-memory SQLite database.
                Startup registers a fixed in-memory schema, then creates scopes, identity resources, clients, and typed
                client extension-property values through the administration APIs.
              </p>

              <h2>Inspect configuration</h2>
              <p><a href="/.well-known/openid-configuration">Open the discovery document</a>.</p>
              <p>
                Complete the user sign-in below, then
                <a href="/sample/clients?search=storage-sample">search clients through IClientAdmin</a>.
              </p>

              <h2>Client credentials</h2>
              <p>
                The custom token request validator compares <code>X-Simulated-Region</code> with the client's stored
                <code>allowed_region</code> property. The header is intentionally caller-controlled for this local demo.
                A production resolver must use trusted deployment or network data instead.
              </p>
              <pre>curl --fail-with-body -X POST "{{encodedBaseUrl}}/connect/token" -H "Content-Type: application/x-www-form-urlencoded" -H "X-Simulated-Region: eu-west" -d "grant_type=client_credentials&amp;client_id={{SampleData.ClientCredentialsClientId}}&amp;client_secret={{SampleData.ClientSecret}}&amp;scope=sample-api"</pre>
              <p>Change the region to <code>us-east</code> to see the extension reject the request.</p>

              <h2>User token and server-side session</h2>
              <p>
                <a href="/sample/login">Start the authorization code flow with PKCE</a>.
                Sign in as <code>alice</code> with password <code>alice</code>. The callback exchanges the code and
                displays the token response.
              </p>
              <p><a href="/sample/sessions">Inspect storage-backed server-side sessions</a> after signing in.</p>
            </body>
            </html>
            """;
    }
}
