// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementPasswordRegistration.Pages;

public class RegisterModel(IOtpSender otpSender, OtpCookie otpCookie) : PageModel
{
    [BindProperty][Required] public string Email { get; set; } = string.Empty;

    [BindProperty] public string? ReturnUrl { get; set; }

    public List<string> ErrorMessages { get; set; } = [];

    public void OnGet(string? returnUrl) => ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!EmailAddress.TryCreate(Email, out var email))
        {
            ModelState.AddModelError(nameof(Email), "Invalid email format");
            return Page();
        }

        if (await otpSender.TrySendOtpAsync(new OtpAddress(OtpChannel.Email, email), HttpContext.RequestAborted)
            is not { } result)
        {
            ErrorMessages.Add("Failed to send OTP.");
            return Page();
        }

        switch (result)
        {
            case SendOtpResult.Sent sentResult:
                otpCookie.Write(sentResult.Token.Value, email, sentResult.ExpiresAtUtc);
                return RedirectToPage("/CompleteRegistration", new { ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/") });
            case SendOtpResult.Blocked blockedResult:
                var blockedFor = blockedResult.SendingBlockedUntilUtc - DateTimeOffset.UtcNow;
                ErrorMessages.Add($"Failed to send OTP. Try again in {Math.Ceiling(blockedFor.TotalSeconds)} second(s).");
                return Page();
            case SendOtpResult.SaveFailed failed:
            default:
                ErrorMessages.Add($"Failed to send OTP.");
                return Page();
        }
    }
}
