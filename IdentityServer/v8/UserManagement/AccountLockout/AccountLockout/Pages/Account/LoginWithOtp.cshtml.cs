// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Services;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AccountLockout.Pages.Account;

public sealed class LoginWithOtpModel(
    IOtpSender otpSender,
    OtpCookie otpCookie,
    IIdentityServerInteractionService interactionService,
    IOptions<AccountLockoutSampleOptions> sampleOptions) : PageModel
{
    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string ReturnUrl { get; set; } = "/";

    public string? ErrorMessage { get; set; }

    public IReadOnlyList<string> SeededEmails { get; } =
    [
        sampleOptions.Value.AdminEmail,
        sampleOptions.Value.UserEmail
    ];

    public void OnGet(string? returnUrl) =>
        ReturnUrl = ReturnUrlHelpers.Normalize(returnUrl, interactionService, Url);

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl = ReturnUrlHelpers.Normalize(ReturnUrl, interactionService, Url);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var emailAddress = EmailAddress.Create(Email);
        var address = new OtpAddress(OtpChannel.Email, emailAddress);
        var result = await otpSender.TrySendOtpAsync(address, HttpContext.RequestAborted);

        if (result is SendOtpResult.Blocked blocked)
        {
            ErrorMessage = $"A code was recently requested. Please wait about {Math.Ceiling(blocked.SendingBlockedFor.TotalMinutes)} minute(s) and try again.";
            return Page();
        }

        if (result is not SendOtpResult.Sent sent)
        {
            ErrorMessage = "We could not send a verification code right now. Please try again.";
            return Page();
        }

        otpCookie.Write(sent.Token, emailAddress, sent.ExpiresAtUtc);
        return RedirectToPage("/Account/VerifyOtp", new { returnUrl = ReturnUrl });
    }
}
