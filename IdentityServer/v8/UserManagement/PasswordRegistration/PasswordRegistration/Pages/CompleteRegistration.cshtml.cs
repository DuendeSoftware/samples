// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement.Authentication.Otp;
using Duende.UserManagement.Profiles;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PasswordRegistration.Pages;

public class CompleteRegistrationModel(
    IOtpAuthenticator authenticator,
    IUserProfileSelfService profileSelfService,
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
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!otpCookie.TryRead(out var token, out var cookieEmailAddress))
        {
            ErrorMessages.Add("OTP expired.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(cookieEmailAddress.Value))
        {
            ErrorMessages.Add($"Error parsing username from {cookieEmailAddress.Value}");
            return Page();
        }

        //Verify user provided information can be used to authenticate
        //ie: OTP is in valid format, and that passwords match and fit the required criteria
        if (!PlainTextOtp.TryCreate(Otp, out var otp))
        {
            ModelState.AddModelError(nameof(Otp), "Invalid OTP format");
            return Page();
        }

        //Authenticate the user with the OTP
        if (await authenticator.TryAuthenticateAsync(otp.Value, token.Value, HttpContext.RequestAborted) is not OtpAuthenticationResult.Success otpResult)
        {
            ErrorMessages.Add("Authentication failed. Please try again.");
            return Page();
        }

        var subjectId = otpResult.UserSubjectId.ToString();
        var emailAddress = otpResult.Address.SubjectId.ToString();

        //Get the account
        var existing = await profileSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted);
        if (existing is not null)
        {
            ErrorMessages.Add("Your account already exists. Please try logging in.");
            return Page();
        }

        //Create the attributes for the new profile
        var schema = await profileSelfService.GetSchemaAsync(HttpContext.RequestAborted);
        var newUserAttributes = new AttributeValueCollection(schema);
        newUserAttributes.Set(UserAttributes.Email, emailAddress);

        //Create the new user profile
        var newProfile = await profileSelfService.TryCreateAsync(subjectId, newUserAttributes.Validate(), HttpContext.RequestAborted);
        if (newProfile is null)
        {
            ErrorMessages.Add("Failed to create user profile. Please try logging in again.");
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

        //OTP Validated and User is signed in
        //  Redirect to /SetPassword page to add a password to their profile
        var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;
        return safeReturnUrl is null
            ? RedirectToPage("/SetPassword")
            : RedirectToPage("/SetPassword", new { returnUrl = safeReturnUrl });
    }
}
