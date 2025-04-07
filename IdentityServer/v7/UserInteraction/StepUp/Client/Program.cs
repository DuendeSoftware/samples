// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();


builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<StepUpHandler>();
builder.Services.AddOpenIdConnectAccessTokenManagement();
builder.Services.AddUserAccessTokenHttpClient("StepUp",
    configureClient: client =>
    {
        client.BaseAddress = new Uri("https://localhost:7001/step-up/");
    }).AddHttpMessageHandler<StepUpHandler>();

builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultScheme = "cookie";
        opt.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("cookie")
    .AddOpenIdConnect("oidc", opt =>
    {
        opt.Authority = "https://localhost:5001";
        opt.ClientId = "step-up";
        opt.ClientSecret = "secret";
        opt.ResponseType = "code";
        opt.Scope.Add("scope1");
        opt.ClaimActions.Remove("acr");
        opt.SaveTokens = true;
        opt.GetClaimsFromUserInfoEndpoint = true;
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters.NameClaimType = "name";
        opt.TokenValidationParameters.RoleClaimType = "role";

        opt.Events.OnRedirectToIdentityProvider = ctx =>
        {
            if (ctx.Properties.Items.ContainsKey("acr_values"))
            {
                ctx.ProtocolMessage.AcrValues = ctx.Properties.Items["acr_values"];
            }
            if (ctx.Properties.Items.ContainsKey("max_age"))
            {
                ctx.ProtocolMessage.MaxAge = ctx.Properties.Items["max_age"];
            }
            return Task.CompletedTask;
        };

        opt.Events.OnRemoteFailure = ctx =>
        {
            if (ctx.Failure?.Data.Contains("error") ?? false)
            {
                var error = ctx.Failure.Data["error"] as string;
                if (error == IdentityModel.OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements)
                {
                    ctx.HandleResponse();
                    ctx.Response.Redirect("/MfaDeclined");
                }
            }
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
