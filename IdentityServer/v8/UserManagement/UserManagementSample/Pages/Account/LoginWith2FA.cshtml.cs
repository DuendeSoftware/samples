// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.UserManagement.Authentication.Totp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginWith2FAModel(
    ITotpAuthenticator totpAuthenticator,
    SecondFactorStateCookie secondFactorStateCookie) : PageModel
{
    [BindProperty]
    [Required]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "Code must be 6 or 7 characters.")]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        if (!secondFactorStateCookie.TryRead(out _))
        {
            return RedirectToPage("/Account/LoginWithPassword", new { ReturnUrl });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!secondFactorStateCookie.TryRead(out var subjectId))
        {
            return RedirectToPage("/Account/LoginWithPassword", new { ReturnUrl });
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Strip spaces and hyphens from code
        var cleanCode = Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        if (!PlainTextTotp.TryCreate(cleanCode, out var totp))
        {
            ErrorMessage = "Invalid code format.";
            return Page();
        }

        var isValid = await totpAuthenticator.TryAuthenticateAsync(
            subjectId, TotpDeviceName.Default, totp, HttpContext.RequestAborted);

        if (!isValid)
        {
            ErrorMessage = "Invalid authenticator code. Please try again.";
            return Page();
        }

        secondFactorStateCookie.Clear();

        var identityServerUser = new IdentityServerUser(subjectId.ToString())
        {
            AdditionalClaims =
            [
                new Claim(JwtClaimTypes.AuthenticationMethod, "mfa")
            ]
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IssuedUtc = DateTimeOffset.UtcNow,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(identityServerUser, authProperties);

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/"));
    }
}
