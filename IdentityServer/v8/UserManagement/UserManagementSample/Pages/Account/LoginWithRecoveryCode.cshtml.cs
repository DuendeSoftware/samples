// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.UserManagement.Authentication.RecoveryCodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginWithRecoveryCodeModel(
    IRecoveryCodeAuthenticator recoveryCodeAuthenticator,
    SecondFactorStateCookie secondFactorStateCookie) : PageModel
{
    [BindProperty]
    [Required]
    public string RecoveryCode { get; set; } = string.Empty;

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

        // Strip spaces and hyphens from the code
        var cleanCode = RecoveryCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        if (!PlainTextRecoveryCode.TryCreate(cleanCode, out var code))
        {
            ErrorMessage = "Invalid recovery code format.";
            return Page();
        }

        var isValid = await recoveryCodeAuthenticator.TryAuthenticateAsync(
            subjectId, code, HttpContext.RequestAborted);

        if (!isValid)
        {
            ErrorMessage = "Invalid recovery code. Each code can only be used once.";
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
