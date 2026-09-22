// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AccountLockout.Pages.Account;

public sealed class LoginModel(
    IIdentityServerInteractionService interactionService,
    IOptions<AccountLockoutSampleOptions> sampleOptions) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    public IReadOnlyList<SeededIdentity> SeededIdentities { get; private set; } = [];

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = ReturnUrlHelpers.Normalize(returnUrl, interactionService, Url);

        var options = sampleOptions.Value;
        SeededIdentities =
        [
            new SeededIdentity(options.AdminName, options.AdminEmail, "Admin"),
            new SeededIdentity(options.UserName, options.UserEmail, "User")
        ];
    }

    public sealed record SeededIdentity(string Name, string Email, string Role);
}
