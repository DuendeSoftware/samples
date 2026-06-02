// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Text;
using Duende.IdentityModel;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.RecoveryCodes;
using Duende.UserManagement.Authentication.Totp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Manage;

[Authorize]
public sealed class Setup2FAModel(
    IUserAuthenticatorsSelfService authenticatorsSelfService) : PageModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "Code must be 6 or 7 characters.")]
    public string Code { get; set; } = string.Empty;

    [TempData]
    public string? TotpKeyBase32 { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (GetUserId() is not { } userId)
        {
            return RedirectToPage("/Account/Login");
        }

        var key = PlainBytesTotpKey.New();
        TotpKeyBase32 = key.EncodeToBase32();

        LoadSharedKeyAndQrCodeUri(key);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (GetUserId() is not { } userId)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            RestoreKeyFromTempData();
            return Page();
        }

        var keyBase32 = TotpKeyBase32;
        if (string.IsNullOrEmpty(keyBase32))
        {
            ErrorMessage = "Session expired. Please start over.";
            return RedirectToPage();
        }

        var key = PlainBytesTotpKey.DecodeFromBase32(keyBase32);

        var cleanCode = Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (!PlainTextTotp.TryCreate(cleanCode, out var totp))
        {
            ErrorMessage = "Invalid code format.";
            TotpKeyBase32 = keyBase32;
            LoadSharedKeyAndQrCodeUri(key);
            return Page();
        }

        var added = await authenticatorsSelfService.TryAddTotpDeviceAsync(
            userId,
            TotpDeviceName.Default,
            key,
            totp,
            HttpContext.RequestAborted);

        if (!added)
        {
            ErrorMessage = "Verification code is invalid. Please try again.";
            TotpKeyBase32 = keyBase32;
            LoadSharedKeyAndQrCodeUri(key);
            return Page();
        }

        var recoveryCodes = await authenticatorsSelfService.TryCreateRecoveryCodesAsync(userId, HttpContext.RequestAborted);
        if (recoveryCodes is { Count: > 0 })
        {
            TempData["RecoveryCodes"] = recoveryCodes.Select(FormatRecoveryCode).ToArray();
            return RedirectToPage("/Manage/ShowRecoveryCodes");
        }

        TempData["StatusMessage"] = "Two-factor authentication has been enabled.";
        return RedirectToPage("/Manage/Setup2FA");
    }

    private void LoadSharedKeyAndQrCodeUri(PlainBytesTotpKey key)
    {
        SharedKey = FormatKeyToHumanReadable(key.EncodeToBase32());
        var accountName = User.FindFirst(JwtClaimTypes.Name)?.Value
            ?? User.FindFirst(JwtClaimTypes.Email)?.Value
            ?? User.Identity?.Name
            ?? "user";
        AuthenticatorUri = TotpAuthenticatorUri.Generate("User Management Sample", accountName, key);
    }

    private void RestoreKeyFromTempData()
    {
        var keyBase32 = TotpKeyBase32;
        if (!string.IsNullOrEmpty(keyBase32))
        {
            LoadSharedKeyAndQrCodeUri(PlainBytesTotpKey.DecodeFromBase32(keyBase32));
        }
    }

    private static string FormatKeyToHumanReadable(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private static string FormatRecoveryCode(PlainTextRecoveryCode code) =>
        string.Join("-", code.ToTextGroups());

    private UserSubjectId? GetUserId()
    {
        if (User.FindFirst(JwtClaimTypes.Subject)?.Value is not { } sub)
        {
            return null;
        }

        return UserSubjectId.Create(sub);
    }
}
