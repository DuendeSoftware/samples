// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace;


public class CallApiModel(IHttpClientFactory httpClientFactory) : PageModel
{
    public string Json = string.Empty;

    public async Task OnGet()
    {
        //var tokenInfo = await HttpContext.GetUserAccessTokenAsync();
        //var client = new HttpClient();
        //client.SetBearerToken(tokenInfo.AccessToken!);

        var client = httpClientFactory.CreateClient("apiClient");

        var content = await client.GetStringAsync("https://localhost:6001/identity");

        var parsed = JsonDocument.Parse(content);
        var formatted = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });

        Json = formatted;
    }
}
