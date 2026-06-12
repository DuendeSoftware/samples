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

namespace UserManagementPasswordRegistration.Pages;

public class CompleteForgotPasswordModel(
    IOtpAuthenticator authenticator,
    IUserAuthenticatorsSelfService authenticatorsSelfService,
    OtpCookie otpCookie)
    : PageModel
{
    public string? Email { get; set; }

    [BindProperty][Required] public string Otp { get; set; } = string.Empty;

    [BindProperty] public string? ReturnUrl { get; set; }

    public List<string> ErrorMessages { get; set; } = [];

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        if (!otpCookie.TryRead(out _, out var emailAddress))
        {
            ErrorMessages.Add("OTP not sent or expired.");
            return Page();
        }

        Email = emailAddress.ToString();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!otpCookie.TryRead(out var token, out var cookieEmailAddress))
        {
            ErrorMessages.Add("OTP expired.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!PlainTextOtp.TryCreate(Otp, out var otp))
        {
            ModelState.AddModelError(nameof(Otp), "Invalid OTP format");
            return Page();
        }

        if (await authenticator.TryAuthenticateAsync(otp.Value, token.Value, HttpContext.RequestAborted) is not OtpAuthenticationResult.Success otpResult
            || otpResult.UserSubjectId is null)
        {
            ErrorMessages.Add("Authentication failed. Please try again.");
            return Page();
        }

        var subjectId = otpResult.UserSubjectId.ToString();
        var emailAddress = otpResult.Address.SubjectId.ToString();

        //Account doesn't exist. Error out, but don't reveal that to the user
        var userAccount = await authenticatorsSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted);
        if (userAccount is null)
        {
            ErrorMessages.Add("Authentication failed. Please try logging in.");
            return Page();
        }

        var identityServerUser = new IdentityServerUser(subjectId)
        {
            AdditionalClaims =
            [
                new Claim(JwtClaimTypes.Email, emailAddress),
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
        otpCookie.Clear();

        var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;
        return safeReturnUrl is null
            ? RedirectToPage("/ResetPassword")
            : RedirectToPage("/ResetPassword", new { returnUrl = safeReturnUrl });
    }
}
