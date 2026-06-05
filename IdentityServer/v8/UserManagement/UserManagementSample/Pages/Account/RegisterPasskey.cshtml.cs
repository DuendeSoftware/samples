// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

[Authorize]
public sealed class RegisterPasskeyModel : PageModel
{
    public string ReturnUrl { get; private set; } = "/";
    public string UserName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        UserName = User.FindFirst(JwtClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.Identity?.Name
            ?? User.FindFirst(JwtClaimTypes.Subject)?.Value
            ?? string.Empty;
        DisplayName = UserName;
        return Page();
    }
}
