// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.Storage.Querying;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Otp;
using Duende.UserManagement.Profiles;
using Duende.UserManagement.Scim;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScimProvisioning.IdentityServer.Pages.Account;

public sealed class LoginModel(
    IUserProfileAdmin profiles,
    IUserAuthenticatorsAdmin authenticators,
    IOtpSender sender,
    OtpCookie cookie) : PageModel
{
    [BindProperty, Required, EmailAddress]
    public string Email { get; set; } = "";

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        cookie.Clear();
        var ct = HttpContext.RequestAborted;
        var profile = await FindProvisionedProfileAsync(Email, ct);
        if (profile is null)
        {
            ModelState.AddModelError("", "Provision this userName from the Directory before signing in.");
            return Page();
        }

        var address = new OtpAddress(OtpChannel.Email, EmailAddress.Create(Email));
        var registered = await authenticators.TryGetAsync(profile.SubjectId, ct)
            ?? await authenticators.TryAddAsync(profile.SubjectId, [address], [], ct);
        if (registered is null || !registered.OtpAddresses.Contains(address))
        {
            ModelState.AddModelError("", "The OTP address could not be attached to this provisioned subject. Check for conflicting authenticator registrations.");
            return Page();
        }

        var result = await sender.TrySendOtpAsync(address, ct);
        if (result is not SendOtpResult.Sent sent)
        {
            ModelState.AddModelError("", result is SendOtpResult.Blocked
                ? "OTP requests are temporarily throttled. Try again later."
                : "The code could not be sent. Check Mailpit and the SMTP configuration.");
            return Page();
        }

        cookie.Write(new OtpCookie.State(sent.Token.ToString(), Email, profile.SubjectId.ToString()), sent.ExpiresAtUtc);
        return RedirectToPage("VerifyOtp");
    }

    // TryGetAsync(userName, value) does not find SCIM-provisioned profiles in 2.0.0-preview.6,
    // so look the profile up with a filtered query and require exactly one exact match.
    private async Task<UserProfile?> FindProvisionedProfileAsync(string userName, CancellationToken ct)
    {
        if (userName.Contains('"') || userName.Contains('\\'))
        {
            return null;
        }

        var filter = SearchExpression.Create($"{DefaultScimUserSchema.UserName.Code} eq \"{userName}\"");
        var result = await profiles.QueryAsync(QueryRequest.Create(FilterBy.FromSearchExpression(filter)), ct);
        var matches = result.Items.Where(profile =>
            profile.Attributes.TryGetValue(DefaultScimUserSchema.UserName.Code, out var value)
            && string.Equals(value.UntypedValue as string, userName, StringComparison.Ordinal)).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
