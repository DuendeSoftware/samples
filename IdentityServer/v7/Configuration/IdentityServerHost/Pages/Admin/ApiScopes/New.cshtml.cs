// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Pages.Admin.ApiScopes;

[SecurityHeaders]
[Authorize]
public class NewModel : PageModel
{
    private readonly ApiScopeRepository _repository;

    public NewModel(ApiScopeRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public ApiScopeModel InputModel { get; set; } = default!;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            await _repository.CreateAsync(InputModel);
            return RedirectToPage("/Admin/ApiScopes/Edit", new { id = InputModel.Name });
        }

        return Page();
    }
}
