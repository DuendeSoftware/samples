// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Storage.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(
    IIdentityServerInteractionService interaction,
    TestUserStore users) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet(string? returnUrl) => Input.ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync()
    {
        var context = await interaction.GetAuthorizationContextAsync(
            Input.ReturnUrl,
            HttpContext.RequestAborted);

        if (context is null && !Url.IsLocalUrl(Input.ReturnUrl))
        {
            return BadRequest("The login request did not contain a valid return URL.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!users.ValidateCredentials(Input.Username, Input.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var user = users.FindByUsername(Input.Username)
            ?? throw new InvalidOperationException("The validated sample user could not be loaded.");

        await HttpContext.SignInAsync(new IdentityServerUser(user.SubjectId)
        {
            DisplayName = user.Username,
            AdditionalClaims = [.. user.Claims.Where(claim => claim.Type == JwtClaimTypes.Role)]
        });

        return Redirect(Input.ReturnUrl ?? "~/");
    }

    public sealed class InputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
