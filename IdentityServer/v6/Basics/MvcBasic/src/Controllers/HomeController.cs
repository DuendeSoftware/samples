// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDiscoveryCache _discoveryCache;

    public HomeController(IHttpClientFactory httpClientFactory, IDiscoveryCache discoveryCache)
    {
        _httpClientFactory = httpClientFactory;
        _discoveryCache = discoveryCache;
    }

    [AllowAnonymous]
    public IActionResult Index() => View();

    public IActionResult Secure() => View();

    public IActionResult Logout() => SignOut("oidc", "Cookies");

    public async Task<IActionResult> CallApi()
    {
        var token = await HttpContext.GetTokenAsync("access_token");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetStringAsync(Urls.SampleApi + "identity");
        var json = JsonDocument.Parse(response);

        ViewBag.Json = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });
        return View();
    }

    public async Task<IActionResult> RefreshToken()
    {
        var disco = await _discoveryCache.GetAsync();
        if (disco.IsError) throw new Exception(disco.Error);

        var rt = await HttpContext.GetTokenAsync("refresh_token");
        var tokenClient = _httpClientFactory.CreateClient();

        var tokenResult = await tokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = "interactive.mvc.sample",
            ClientSecret = "secret",
            RefreshToken = rt
        });

        if (!tokenResult.IsError)
        {
            var oldIdToken = await HttpContext.GetTokenAsync("id_token");
            var newAccessToken = tokenResult.AccessToken;
            var newRefreshToken = tokenResult.RefreshToken;
            var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);

            var info = await HttpContext.AuthenticateAsync("Cookies");

            info.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
            info.Properties.UpdateTokenValue("access_token", newAccessToken);
            info.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

            await HttpContext.SignInAsync("Cookies", info.Principal, info.Properties);
            return Redirect("~/Home/Secure");
        }

        ViewData["Error"] = tokenResult.Error;
        return View("Error");
    }
}
