using Duende.Bff.Blazor.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBffBlazorClient(opt => opt.RemoteApiPath = "remote-apis/greetings/")
    .AddCascadingAuthenticationState();

builder.Services.AddSingleton<IWeatherClient>(sp => sp.GetRequiredService<WeatherClient>());

builder.Services.AddLocalApiHttpClient<WeatherClient>();

builder.Services.AddRemoteApiHttpClient("greet");

await builder.Build().RunAsync();
