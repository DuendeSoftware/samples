// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebClient.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [AllowAnonymous]
    public IActionResult Index() => View();

    public IActionResult Secure() => View();

    public async Task<IActionResult> Renew()
    {
        await HttpContext.GetUserAccessTokenAsync(new UserTokenRequestParameters { ForceTokenRenewal = true });
        return RedirectToAction(nameof(Secure));
    }

    public IActionResult Logout() => SignOut("oidc");

    public async Task<IActionResult> CallApi()
    {
        var client = _httpClientFactory.CreateClient("client");

        var response = await client.GetStringAsync("identity");
        ViewBag.Json = response.PrettyPrintJson();

        return View();
    }


}
