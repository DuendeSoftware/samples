// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountLockout.Pages.Account;

[Authorize]
public sealed class RegisterPasskeyModel(IIdentityServerInteractionService interactionService) : PageModel
{
    public string ReturnUrl { get; private set; } = "/";
    public string DisplayName { get; private set; } = string.Empty;

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = ReturnUrlHelpers.Normalize(returnUrl, interactionService, Url);
        DisplayName = User.FindFirst(JwtClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(JwtClaimTypes.Email)?.Value
            ?? User.FindFirst(JwtClaimTypes.Subject)?.Value
            ?? string.Empty;
        return Page();
    }
}
