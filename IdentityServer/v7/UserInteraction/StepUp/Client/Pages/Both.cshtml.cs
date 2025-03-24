// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Client;

[Authorize]
public class BothModel : PageModel
{
    public BothModel(IHttpClientFactory clientFactory)
    {
        _http = clientFactory.CreateClient("StepUp");
    }

    private readonly HttpClient _http;

    public string? ApiResponse { get; private set; }

    public async Task OnGet()
    {
        var response = await _http.GetAsync("both");
        if (response.IsSuccessStatusCode)
        {
            ApiResponse = (await response.Content.ReadAsStringAsync())
                .PrettyPrintJson();
        }
    }
}
