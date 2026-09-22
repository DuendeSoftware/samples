// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AccountLockout.Services;
using Duende.IdentityModel;
using Duende.IdentityServer.Services;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Otp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountLockout.Pages.Account;

public sealed class VerifyOtpModel(
    IOtpAuthenticator otpAuthenticator,
    IUserAuthenticatorsSelfService authenticatorsSelfService,
    ApplicationSessionService sessionService,
    AccountLockoutService accountLockoutService,
    OtpCookie otpCookie,
    IIdentityServerInteractionService interactionService) : PageModel
{
    public string Email { get; private set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public string ReturnUrl { get; set; } = "/";

    public string? ErrorMessage { get; set; }

    public string? LockoutMessage { get; private set; }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = ReturnUrlHelpers.Normalize(returnUrl, interactionService, Url);

        if (!otpCookie.TryRead(out _, out var emailAddress))
        {
            ErrorMessage = "Your OTP session expired. Request a new code and try again.";
            return Page();
        }

        Email = emailAddress.ToString();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = ReturnUrlHelpers.Normalize(ReturnUrl, interactionService, Url);

        if (!otpCookie.TryRead(out var token, out var emailAddress))
        {
            ErrorMessage = "Your OTP session expired. Request a new code and try again.";
            return Page();
        }

        Email = emailAddress.ToString();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var authenticationResult = await otpAuthenticator.TryAuthenticateAsync(PlainTextOtp.Create(Code), token, HttpContext.RequestAborted);
        if (authenticationResult is not OtpAuthenticationResult.Success success)
        {
            ErrorMessage = "The verification code was invalid or expired. Please try again.";
            return Page();
        }

        otpCookie.Clear();

        var profile = await sessionService.EnsureProfileAsync(success.UserSubjectId, emailAddress.ToString(), HttpContext.RequestAborted);
        if (profile is null)
        {
            ErrorMessage = "The account profile could not be created. Please try again.";
            return Page();
        }

        // Check only after OTP authentication succeeds. Checking by email before this point
        // would let unauthenticated callers distinguish locked accounts from unknown accounts.
        var lockoutState = accountLockoutService.GetState(profile);
        if (lockoutState.IsLocked)
        {
            LockoutMessage = accountLockoutService.Describe(lockoutState);
            return Page();
        }

        await sessionService.SignInAsync(HttpContext, profile, OidcConstants.AuthenticationMethods.OneTimePassword);

        var authenticators = await authenticatorsSelfService.TryGetAsync(success.UserSubjectId, HttpContext.RequestAborted);
        var hasPasskey = authenticators?.Passkeys.Count > 0;

        if (!hasPasskey)
        {
            return RedirectToPage("/Account/RegisterPasskey", new { returnUrl = ReturnUrl });
        }

        return Redirect(ReturnUrl);
    }
}
