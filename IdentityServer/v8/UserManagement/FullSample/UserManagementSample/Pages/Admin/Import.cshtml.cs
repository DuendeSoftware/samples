using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserManagementSample.Import;

namespace UserManagementSample.Pages.Admin;

[Authorize]
public sealed class ImportModel(AspNetIdentityImporter importer, IWebHostEnvironment env) : PageModel
{
    public ImportResult? Result { get; private set; }

    public IActionResult OnGet()
    {
        if (!env.IsDevelopment())
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        Result = await importer.ImportFromIdentityDbAsync(ct);
        return Page();
    }
}
