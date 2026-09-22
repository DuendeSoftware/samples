// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AccountLockout.Services;
using Duende.IdentityModel;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passkeys;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountLockout.Pages.Account;

[Authorize]
public sealed class ProfileModel(
    IUserProfileSelfService profileSelfService,
    IUserAuthenticatorsSelfService authenticatorsSelfService,
    AccountLockoutService accountLockoutService) : PageModel
{
    public string SubjectId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<UserPasskey> Passkeys { get; private set; } = [];
    public string LockStateDescription { get; private set; } = string.Empty;
    public string LockStateCssClass { get; private set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (GetUserId() is not { } subjectId)
        {
            return RedirectToPage("/Account/Login");
        }

        var profile = await profileSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted);
        if (profile is null)
        {
            return RedirectToPage("/Account/Login");
        }

        var authenticators = await authenticatorsSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted);
        var lockoutState = accountLockoutService.GetState(profile);

        SubjectId = subjectId.ToString();
        Email = profile.TryGetString(AccountLockoutProfileAttributes.Email.Code) ?? "(not available)";
        Name = profile.TryGetString(AccountLockoutProfileAttributes.Name.Code) ?? "(not available)";
        Passkeys = authenticators?.Passkeys ?? [];
        LockStateDescription = accountLockoutService.Describe(lockoutState);
        LockStateCssClass = lockoutState.Kind switch
        {
            AccountLockoutKind.IndefinitelyLocked => "text-bg-danger",
            AccountLockoutKind.TemporarilyLocked => "text-bg-warning",
            _ => "text-bg-success"
        };

        return Page();
    }

    public async Task<IActionResult> OnPostRemovePasskeyAsync(string? credentialId)
    {
        if (GetUserId() is not { } subjectId)
        {
            return RedirectToPage("/Account/Login");
        }

        if (string.IsNullOrWhiteSpace(credentialId))
        {
            ErrorMessage = "The selected passkey identifier was invalid.";
            return RedirectToPage();
        }

        var buffer = new byte[credentialId.Length];
        if (!Convert.TryFromBase64String(credentialId, buffer, out var bytesWritten)
            || !PasskeyCredentialId.TryFrom(buffer[..bytesWritten], out var passkeyId))
        {
            ErrorMessage = "The selected passkey identifier was invalid.";
            return RedirectToPage();
        }

        if (!await authenticatorsSelfService.TryRemovePasskeyAsync(subjectId, passkeyId.Value, HttpContext.RequestAborted))
        {
            ErrorMessage = "The passkey could not be removed.";
            return RedirectToPage();
        }

        SuccessMessage = "Passkey removed.";
        return RedirectToPage();
    }

    private UserSubjectId? GetUserId()
    {
        if (User.FindFirst(JwtClaimTypes.Subject)?.Value is not { } sub)
        {
            return null;
        }

        return UserSubjectId.Create(sub);
    }
}
