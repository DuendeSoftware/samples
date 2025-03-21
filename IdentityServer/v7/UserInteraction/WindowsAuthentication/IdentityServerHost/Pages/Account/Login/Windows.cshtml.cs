// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using System.Security.Principal;
using Duende.IdentityServer;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Account.Login;

public class WindowsModel : PageModel
{
    public async Task<IActionResult> OnGet(string returnUrl)
    {
        // see if windows auth has already been requested and succeeded
        var result = await HttpContext.AuthenticateAsync("Windows");
        if (result?.Principal is WindowsPrincipal wp)
        {
            // beware the performance penalty for loading these group claims
            var wi = wp.Identity as WindowsIdentity;
            var groups = wi.Groups.Translate(typeof(NTAccount));
            var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));

            var user = new IdentityServerUser(wp.FindFirst(ClaimTypes.PrimarySid).Value)
            {
                IdentityProvider = "Windows",
                DisplayName = wp.Identity.Name,
                AdditionalClaims = roles.ToList(),
            };

            await HttpContext.SignInAsync(user);
            return LocalRedirect(returnUrl);
        }
        else
        {
            // trigger windows auth
            // since windows auth don't support the redirect uri,
            // this URL is re-triggered when we call challenge
            return Challenge("Windows");
        }
    }
}
