// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginWithOtpModel(
    IOtpSender otpSender,
    OtpCookie otpCookie) : PageModel
{
    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl) =>
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var address = new OtpAddress(OtpChannel.Email, EmailAddress.Create(Email));
        var result = await otpSender.TrySendOtpAsync(address, HttpContext.RequestAborted);

        if (result is SendOtpResult.Blocked blocked)
        {
            ErrorMessage = $"Too many attempts. Please try again in {Math.Ceiling(blocked.SendingBlockedFor.TotalSeconds)} second(s).";
            return Page();
        }

        if (result is not SendOtpResult.Sent sent)
        {
            ErrorMessage = "Could not send a verification code to that email address.";
            return Page();
        }

        otpCookie.Write(sent.Token.Value, (EmailAddress)address.SubjectId, sent.ExpiresAtUtc);

        return RedirectToPage("/Account/VerifyOtp", new { ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/") });
    }
}
