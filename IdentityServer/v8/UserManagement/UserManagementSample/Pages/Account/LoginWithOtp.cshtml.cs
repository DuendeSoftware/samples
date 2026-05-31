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

        if (result is null)
        {
            ErrorMessage = "Could not send a verification code to that email address.";
            return Page();
        }

        if (!result.Sent)
        {
            ErrorMessage = $"Too many attempts. Please try again in {Math.Ceiling(result.SendingBlockedFor.TotalSeconds)} second(s).";
            return Page();
        }

        otpCookie.Write(result.Token.Value, (EmailAddress)address.SubjectId, result.ExpiresAtUtc!.Value);

        return RedirectToPage("/Account/VerifyOtp", new { ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/") });
    }
}
