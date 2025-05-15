// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Services;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Mfa;

public class Index : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;

    public Index(IIdentityServerInteractionService interaction)
    {
        _interaction = interaction;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public ViewModel View { get; set; }

    public async Task OnGetAsync(string returnUrl)
    {
        await BuildModelAsync(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var existingProps = (await HttpContext.AuthenticateAsync()).Properties;

            var claims = Input.Button == "fake" ?
                User.Claims
                    .Append(new Claim(JwtClaimTypes.AuthenticationMethod, "mfa"))
                    .Append(new Claim(JwtClaimTypes.AuthenticationContextClassReference, "1")) :
                User.Claims
                    .Append(new Claim("declined_mfa", "true"));
            var newPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "mfa", "name", "role"));
            await HttpContext.SignInAsync(newPrincipal, existingProps);

            var authContext = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
            if (authContext != null)
            {
                // Safe to trust input, because authContext is non-null
                return Redirect(Input.ReturnUrl);
            }
        }
        // something went wrong, show form with error
        await BuildModelAsync(Input.ReturnUrl);
        return Page();
    }

    public async Task BuildModelAsync(string returnUrl)
    {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

        if (context != null)
        {
            Input = new InputModel
            {
                ReturnUrl = returnUrl
            };

            View = new ViewModel
            {
                ClientName = context.Client.ClientName,
                MfaRequestedByClient = context.AcrValues.Contains("mfa")
            };
        }

    }
}
