// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;

namespace MauiApp1;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddLogging(configure => configure.AddDebug());
#endif

        // setup OidcClient
        builder.Services.AddSingleton(new OidcClient(new()
        {
            Authority = "https://demo.duendesoftware.com",

            ClientId = "interactive.public",
            Scope = "openid profile api",
            RedirectUri = "myapp://callback",

            Browser = new MauiAuthenticationBrowser()
        }));

        // add main page
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
