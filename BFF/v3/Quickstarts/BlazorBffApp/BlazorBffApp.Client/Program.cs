// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Bff.Blazor.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBffBlazorClient()// Provides auth state provider that polls the /bff/user endpoint

    // Register a HTTP Client that's configured to fetch data from the server. 
    .AddLocalApiHttpClient<WeatherHttpClient>();

// Register the concrete implementation with the abstraction
builder.Services.AddSingleton<IWeatherClient, WeatherHttpClient>();

builder.Services
    .AddCascadingAuthenticationState();

await builder.Build().RunAsync();
