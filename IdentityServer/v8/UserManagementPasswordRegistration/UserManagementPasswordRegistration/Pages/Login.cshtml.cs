// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Passwords;
using Duende.UserManagement.Profiles;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementPasswordRegistration.Pages;

public class LoginModel(IPasswordAuthenticator passwordAuth) : PageModel
{
    [BindProperty][Required] public string Email { get; set; } = string.Empty;
    [BindProperty][Required] public string Password { get; set; } = string.Empty;

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

        if (!NonValidatedPassword.TryCreate(Password, out var passwordResult))
        {
            ErrorMessages.Add("Failed to sign in with your username/password combo");
            return Page();
        }

        var authResult = await passwordAuth.TryAuthenticateAsync(OidcStandardAttributes.Email, email, passwordResult, HttpContext.RequestAborted);
        if (authResult is not PasswordAuthenticationResult.Success authSuccess)
        {
            ErrorMessages.Add("Failed to sign in with your username/password combo");
            return Page();
        }

        var identityServerUser = new IdentityServerUser(authSuccess.UserSubjectId.Value)
        {
            AdditionalClaims =
            [
                new Claim(JwtClaimTypes.Email, email.Value),
                new Claim(JwtClaimTypes.AuthenticationMethod, OidcConstants.AuthenticationMethods.Password)
            ]
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IssuedUtc = DateTimeOffset.UtcNow,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(identityServerUser, authProperties);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/"));
    }
}
