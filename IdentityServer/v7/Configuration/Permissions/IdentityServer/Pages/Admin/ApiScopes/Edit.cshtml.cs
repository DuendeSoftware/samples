// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Pages.Admin.ApiScopes;

[SecurityHeaders]
[Authorize]
public class EditModel : PageModel
{
    private readonly ApiScopeRepository _repository;

    public EditModel(ApiScopeRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public ApiScopeModel InputModel { get; set; } = default!;
    [BindProperty]
    public string? Button { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var apiScope = await _repository.GetByIdAsync(id);
        if (apiScope == null)
        {
            return RedirectToPage("/Admin/ApiScopes/Index");
        }
        else
        {
            InputModel = apiScope;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (Button == "delete")
        {
            await _repository.DeleteAsync(id);
            return RedirectToPage("/Admin/ApiScopes/Index");
        }

        if (ModelState.IsValid)
        {
            await _repository.UpdateAsync(InputModel);
            return RedirectToPage("/Admin/ApiScopes/Edit", new { id });
        }

        return Page();
    }
}
