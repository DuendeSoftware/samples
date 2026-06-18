// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PasswordRegistration.Pages;

public class LogoutModel : PageModel
{
    [BindProperty] public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl) => ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/"));
    }
}
