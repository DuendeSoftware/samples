// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScimProvisioning.IdentityServer.Pages.Account;

public sealed class LogoutModel(OtpCookie cookie) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        cookie.Clear();
        await HttpContext.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
