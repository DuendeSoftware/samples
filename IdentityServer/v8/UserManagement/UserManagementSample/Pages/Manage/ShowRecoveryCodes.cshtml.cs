// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Manage;

[Authorize]
public sealed class ShowRecoveryCodesModel : PageModel
{
    [TempData]
    public string[]? RecoveryCodes { get; set; }

    public IActionResult OnGet()
    {
        if (RecoveryCodes is not { Length: > 0 })
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }
}
