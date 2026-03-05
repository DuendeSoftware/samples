// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Pages.Admin.Clients;

[SecurityHeaders]
[Authorize]
public class EditModel : PageModel
{
    private readonly ClientRepository _repository;

    public EditModel(ClientRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public ClientModel InputModel { get; set; } = default!;
    [BindProperty]
    public string? Button { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var client = await _repository.GetByIdAsync(id);
        if (client == null)
        {
            return RedirectToPage("/Admin/Clients/Index");
        }
        else
        {
            InputModel = client;
            return Page();
        }

    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (Button == "delete")
        {
            await _repository.DeleteAsync(id);
            return RedirectToPage("/Admin/Clients/Index");
        }

        if (ModelState.IsValid)
        {
            ArgumentNullException.ThrowIfNull(InputModel);
            await _repository.UpdateAsync(InputModel);
            return RedirectToPage("/Admin/Clients/Edit", new { id });
        }

        return Page();
    }
}
