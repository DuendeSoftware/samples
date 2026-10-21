// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;
using Duende.UserManagement.Profiles;
using Duende.UserManagement.Scim;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScimProvisioning.IdentityServer.Pages.Account;

public sealed class VerifyOtpModel(IOtpAuthenticator authenticator, IUserProfileAdmin profiles, OtpCookie cookie) : PageModel
{
    [BindProperty, Required]
    public string Code { get; set; } = "";

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var state = cookie.Read();
        if (state is null)
        {
            ModelState.AddModelError("", "The OTP request expired. Request a new code.");
            return Page();
        }

        var ct = HttpContext.RequestAborted;
        var expectedSubject = UserSubjectId.Create(state.SubjectId);
        var profile = await profiles.TryGetAsync(expectedSubject, ct);
        var registeredName = profile?.Attributes.TryGetValue(DefaultScimUserSchema.UserName.Code, out var name) == true
            ? name.UntypedValue?.ToString() : null;
        if (profile is null || !string.Equals(registeredName, state.Email, StringComparison.Ordinal))
        {
            cookie.Clear();
            ModelState.AddModelError("", "The provisioned profile was removed or its userName changed. Request a new code.");
            return Page();
        }

        var result = await authenticator.TryAuthenticateAsync(PlainTextOtp.Create(Code), OtpToken.Create(state.Token), ct);
        if (result is not OtpAuthenticationResult.Success success)
        {
            ModelState.AddModelError("", "The verification code was invalid or expired.");
            return Page();
        }

        cookie.Clear();
        if (success.UserSubjectId != expectedSubject || success.Address != new OtpAddress(OtpChannel.Email, EmailAddress.Create(state.Email)))
        {
            ModelState.AddModelError("", "The authenticated subject does not match the provisioned profile. No session was created.");
            return Page();
        }

        // active is profile data, not an authentication or session-revocation policy.
        await HttpContext.SignInAsync(new IdentityServerUser(profile.SubjectId.ToString())
        {
            DisplayName = registeredName,
            AuthenticationMethods = ["otp"]
        });
        return RedirectToPage("/Index");
    }
}
