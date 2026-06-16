// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

using Duende.IdentityModel;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passwords;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementPasswordRegistration.Pages;

[Authorize]
public class ResetPasswordModel(
    IUserAuthenticatorsSelfService authenticatorsSelfService)
    : PageModel
{
    [BindProperty][Required] public string Password { get; set; } = string.Empty;
    [BindProperty][Required] public string PasswordConfirmation { get; set; } = string.Empty;
    [BindProperty] public string? ReturnUrl { get; set; }

    public List<string> ErrorMessages { get; set; } = [];

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!UserSubjectId.TryCreate(User.FindFirst(JwtClaimTypes.Subject)?.Value, out var userSubjectId))
        {
            ModelState.AddModelError(nameof(UserSubjectId), "User is not signed in");
            return Page();
        }

        if (!string.Equals(Password, PasswordConfirmation, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(Password), "Passwords do not match");
            return Page();
        }

        var passwordResult = await authenticatorsSelfService.TryValidatePasswordAsync(userSubjectId, Password, HttpContext.RequestAborted);
        if (passwordResult is PasswordCreationResult.Failed failedPassword)
        {
            var errors = string.Join(", ", failedPassword.Errors);
            ModelState.AddModelError(nameof(Password), $"Password doesn't meet the required criteria because: {errors}");
            return Page();
        }

        if (passwordResult is not PasswordCreationResult.Success successPassword)
        {
            ErrorMessages.Add("Error creating password. Please try again.");
            return Page();
        }

        //Store the password to the user's account
        if (!await authenticatorsSelfService.TryResetPasswordAsync(userSubjectId, successPassword.Password, HttpContext.RequestAborted))
        {
            ErrorMessages.Add($"Error setting password for user subhect id {userSubjectId}");
            return Page();
        }

        //Password has been set, sign out to the user so they must sign in again with the new password
        await HttpContext.SignOutAsync();

        var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;
        return safeReturnUrl is null
            ? RedirectToPage("/Login")
            : RedirectToPage("/Login", new { returnUrl = safeReturnUrl });
    }
}
