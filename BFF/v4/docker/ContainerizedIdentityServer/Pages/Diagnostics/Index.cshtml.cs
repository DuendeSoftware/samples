// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace ContainerizedIdentityServer.Pages.Diagnostics;

[SecurityHeaders]
[Authorize]
public class Index : PageModel
{
    public ViewModel View { get; set; } = default!;

    public async Task<IActionResult> OnGet()
    {
        View = new ViewModel(await HttpContext.AuthenticateAsync());
       
        if (View.Clients is null)
        {
           
             return RedirectToPage("/Account/Login", new { returnUrl = "/Diagnostics" });
        }
        
        // Todo: this should be turned off for production. Maybe something like below
        // some info about this can be found here: https://github.com/DuendeArchive/Support/issues/903
        // #if !DEBUG
        //       return Page();  
        // #endif
        return Page();
    }
    
}
