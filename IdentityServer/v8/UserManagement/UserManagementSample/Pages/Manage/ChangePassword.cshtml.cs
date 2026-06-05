// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.IdentityModel;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passwords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Manage;

[Authorize]
public sealed class ChangePasswordModel(
    IUserAuthenticatorsSelfService authenticatorsSelfService) : PageModel
{
    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    [TempData]
    public string? SuccessMessage { get; set; }

    public List<string> ErrorMessages { get; } = [];

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (GetUserId() is not { } userId)
        {
            return RedirectToPage("/Account/Login");
        }

        var creationResult = await authenticatorsSelfService.TryValidatePasswordAsync(
            userId, NewPassword, HttpContext.RequestAborted);

        if (creationResult is PasswordCreationResult.Failed { Errors: var errors })
        {
            foreach (var error in errors)
            {
                ErrorMessages.Add(error);
            }
            return Page();
        }

        var newPassword = ((PasswordCreationResult.Success)creationResult).Password;
        var oldPassword = NonValidatedPassword.Create(CurrentPassword);

        var changed = await authenticatorsSelfService.TryChangePasswordAsync(
            userId, oldPassword, newPassword, HttpContext.RequestAborted);

        if (!changed)
        {
            ErrorMessages.Add("Could not change password. Please check your current password and try again.");
            return Page();
        }

        SuccessMessage = "Password changed successfully.";
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
