// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passwords;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginWithPasswordModel(
    IPasswordAuthenticator passwordAuthenticator,
    IUserAuthenticatorsSelfService authenticatorsSelfService,
    TotpStateCookie totpStateCookie,
    IWebHostEnvironment environment) : PageModel
{
    public bool ShowTestCredentials => environment.IsDevelopment();

    [BindProperty]
    [Required]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

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

        var password = NonValidatedPassword.Create(Password);

        if (await passwordAuthenticator.TryAuthenticateAsync(
                OidcStandardAttributes.Email,
                Email,
                password,
                HttpContext.RequestAborted) is not PasswordAuthenticationResult.Success { UserSubjectId: var subjectId })
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        var authenticators = await authenticatorsSelfService.TryGetAsync(subjectId, HttpContext.RequestAborted);

        if (authenticators?.TotpDeviceNames.Count > 0)
        {
            // Store interim subject ID and redirect to 2FA
            totpStateCookie.Write(subjectId);
            return RedirectToPage("/Account/LoginWith2FA", new
            {
                ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/")
            });
        }

        // No TOTP configured — complete sign-in directly
        var identityServerUser = new IdentityServerUser(subjectId.ToString())
        {
            AdditionalClaims =
            [
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

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Content("~/"));
    }
}
