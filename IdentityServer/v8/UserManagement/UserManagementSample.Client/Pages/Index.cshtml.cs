// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Client.Pages;

public sealed class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private readonly ILogger<IndexModel> _logger = logger;

    public void OnGet()
    {
    }

    public IActionResult OnGetLogin()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "oidc");
    }

    public IActionResult OnPost()
    {
        return SignOut("cookies", "oidc");
    }
}
