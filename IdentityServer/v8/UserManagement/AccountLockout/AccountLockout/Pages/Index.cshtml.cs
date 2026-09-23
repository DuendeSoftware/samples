// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountLockout.Pages;

public sealed class IndexModel : PageModel
{
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SubjectId { get; private set; } = string.Empty;
    public string AuthenticationMethod { get; private set; } = string.Empty;

    public void OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        DisplayName = User.FindFirst(JwtClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "(unknown)";
        Email = User.FindFirst(JwtClaimTypes.Email)?.Value ?? "(not available)";
        SubjectId = User.FindFirst(JwtClaimTypes.Subject)?.Value ?? string.Empty;
        AuthenticationMethod = User.FindFirst(JwtClaimTypes.AuthenticationMethod)?.Value ?? "(not available)";
    }
}
