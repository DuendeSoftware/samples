// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.GettingStarted.Pages.Account;

public class LogoutModel(IIdentityServerInteractionService interaction) : PageModel
{
    public async Task<IActionResult> OnPostAsync(string? logoutId)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);

        await HttpContext.SignOutAsync(
            Duende.IdentityServer.IdentityServerConstants.DefaultCookieAuthenticationScheme);

        var postLogoutRedirect = context?.PostLogoutRedirectUri;
        if (!string.IsNullOrEmpty(postLogoutRedirect))
        {
            return Redirect(postLogoutRedirect);
        }

        return RedirectToPage("/Account/Login");
    }
}
