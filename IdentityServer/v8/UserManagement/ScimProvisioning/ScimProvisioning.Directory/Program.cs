// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using Duende.AccessTokenManagement;
using Microsoft.Extensions.Http.Resilience;
using ScimProvisioning.Directory;

var builder = WebApplication.CreateBuilder(args);
var authority = RequireHttpsUrl("Scim:Authority");
var baseUrl = RequireHttpsUrl("Scim:BaseUrl");
if (!baseUrl.AbsoluteUri.EndsWith('/'))
{
    throw new InvalidOperationException("Scim:BaseUrl must end with '/' so relative SCIM resource URLs resolve correctly.");
}

var clientId = builder.Configuration["Scim:ClientId"];
var secret = builder.Configuration["Scim:ClientSecret"];
if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
{
    throw new InvalidOperationException("Scim:ClientId and Scim:ClientSecret are required.");
}

builder.AddServiceDefaults();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddClientCredentialsTokenManagement()
    .AddClient("scim", options =>
    {
        options.TokenEndpoint = new Uri(authority, "/connect/token");
        options.ClientId = ClientId.Parse(clientId);
        options.ClientSecret = ClientSecret.Parse(secret);
        options.Scope = Scope.Parse("scim");
    });

var http = builder.Services.AddClientCredentialsHttpClient("scim", ClientCredentialsClientName.Parse("scim"), client =>
{
    client.BaseAddress = baseUrl;
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/scim+json"));
});

#pragma warning disable EXTEXP0001
// Keep standard resilience handling for safe requests, but do not retry unsafe methods.
// A SCIM POST (including Bulk) may have committed before its response was lost; replaying
// it could create duplicates, and Bulk may already have partially committed.

http.RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(options =>
        options.Retry.DisableForUnsafeHttpMethods()
    );

#pragma warning restore EXTEXP0001

http.AddTypedClient<ScimClient>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.MapDefaultEndpoints();
app.Run();

Uri RequireHttpsUrl(string key)
{
    var value = builder.Configuration[key];
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
        || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
    {
        throw new InvalidOperationException($"{key} must be an absolute HTTPS URL without credentials, query, or fragment.");
    }
    return uri;
}
