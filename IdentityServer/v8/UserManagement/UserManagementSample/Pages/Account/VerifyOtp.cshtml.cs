// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Otp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class VerifyOtpModel(
    IOtpAuthenticator otpAuthenticator,
    IUserAuthenticatorsSelfService authenticatorsSelfService,
    OtpCookie otpCookie) : PageModel
{
    public string? Email { get; set; }

    [BindProperty]
    [Required]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        if (!otpCookie.TryRead(out _, out var emailAddress))
        {
            ErrorMessage = "OTP not sent or expired. Please sign in again.";
            return Page();
        }

        Email = emailAddress.ToString();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!otpCookie.TryRead(out var token, out var emailAddress))
        {
            ErrorMessage = "OTP expired. Please sign in again.";
            return Page();
        }

        Email = emailAddress.ToString();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var otp = PlainTextOtp.Create(Code);
        if (await otpAuthenticator.TryAuthenticateAsync(otp, token.Value, HttpContext.RequestAborted) is not OtpAuthenticationResult.Success authResult)
        {
            ErrorMessage = "Invalid or expired verification code. Please try again.";
            return Page();
        }

        otpCookie.Clear();

        var subjectId = authResult.UserSubjectId;

        var identityServerUser = new IdentityServerUser(subjectId.ToString())
        {
            AdditionalClaims =
            [
                new Claim(JwtClaimTypes.Email, authResult.Address.SubjectId.ToString()),
                new Claim(JwtClaimTypes.AuthenticationMethod, OidcConstants.AuthenticationMethods.OneTimePassword)
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

        // Check whether the user has a passkey registered; if not, prompt registration
        var authenticators = await authenticatorsSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted);
        var hasPasskey = authenticators?.Passkeys.Count > 0;

        var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/");

        if (!hasPasskey)
        {
            return RedirectToPage("/Account/RegisterPasskey", new { ReturnUrl = safeReturnUrl });
        }

        return LocalRedirect(safeReturnUrl);
    }
}
