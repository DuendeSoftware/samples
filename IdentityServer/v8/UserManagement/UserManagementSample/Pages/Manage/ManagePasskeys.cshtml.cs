// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passkeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Manage;

[Authorize]
public sealed class ManagePasskeysModel(
    IUserAuthenticatorsSelfService authenticatorsSelfService) : PageModel
{
    public IReadOnlyCollection<UserPasskey> Passkeys { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    public List<string> ErrorMessages { get; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPasskeysAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemovePasskeyAsync(string credentialId)
    {
        if (GetUserId() is not { } userId)
        {
            return RedirectToPage("/Account/Login");
        }

        var id = PasskeyCredentialId.From(Convert.FromBase64String(credentialId));

        if (!await authenticatorsSelfService.TryRemovePasskeyAsync(userId, id, HttpContext.RequestAborted))
        {
            ErrorMessages.Add("Failed to remove passkey.");
            await LoadPasskeysAsync();
            return Page();
        }

        SuccessMessage = "Passkey removed successfully.";
        return RedirectToPage();
    }

    private async Task LoadPasskeysAsync()
    {
        if (GetUserId() is not { } userId)
        {
            ErrorMessages.Add("Unable to identify user. Please log in again.");
            return;
        }

        var authenticators = await authenticatorsSelfService.TryGetAsync(userId, HttpContext.RequestAborted);
        if (authenticators is null)
        {
            ErrorMessages.Add("Failed to load authenticators. Please try again.");
            return;
        }

        Passkeys = authenticators.Passkeys;
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
