// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Redirect
{
    public class IndexModel : PageModel
    {
        public string RedirectUri { get; set; }

        public IActionResult OnGet(string redirectUri)
        {
            if (!Url.IsLocalUrl(redirectUri))
            {
                return RedirectToPage("/Error/Index");
            }

            RedirectUri = redirectUri;
            return Page();
        }
    }
}
