// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GettingStarted.Pages.Account;

public class LoginModel(IOtpSender otpSender) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!EmailAddress.TryCreate(Input.Email, out var email))
        {
            ModelState.AddModelError(nameof(Input.Email), "Invalid email format.");
            return Page();
        }

        var result = await otpSender.TrySendOtpAsync(
            new OtpAddress(OtpChannel.Email, email),
            HttpContext.RequestAborted);

        if (result is SendOtpResult.Sent sentResult)
        {
            TempData["OtpToken"] = sentResult.Token.Value.ToString();
            return RedirectToPage("/Account/EnterOtp");
        }

        if (result is SendOtpResult.Blocked blocked)
        {
            var blockedFor = blocked.SendingBlockedUntilUtc - DateTimeOffset.UtcNow;
            var blockedMessage = $"Too many attempts. Try again in {Math.Ceiling(blockedFor.TotalSeconds)} second(s).";
            ModelState.AddModelError(string.Empty, blockedMessage);
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Failed to send one-time password.");
        return Page();
    }
}
