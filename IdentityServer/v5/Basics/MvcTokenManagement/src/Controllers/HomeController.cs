// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Client.Controllers
{
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

        public IActionResult Logout() => SignOut("oidc");

        public async Task<IActionResult> CallApi()
        {
            // retrieve client with token management from HTTP client factory
            // repeat the API call to see that token a requested automatically (e.g. the iat and exp values slide)
            var client = _httpClientFactory.CreateClient("client");
            var response = await client.GetStringAsync("identity");

            var json = JsonDocument.Parse(response);
            ViewBag.Json = JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true });

            return View();
        }
    }
}
