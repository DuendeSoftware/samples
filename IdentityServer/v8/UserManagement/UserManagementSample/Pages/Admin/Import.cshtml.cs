using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UserManagementSample.Import;

namespace UserManagementSample.Pages.Admin;

[Authorize]
public sealed class ImportModel : PageModel
{
    private readonly ILocalUserImporter _importer;
    private readonly IWebHostEnvironment _env;

    public ImportModel(ILocalUserImporter importer, IWebHostEnvironment env)
    {
        _importer = importer;
        _env = env;
    }

    public ImportResult? Result { get; private set; }

    public IActionResult OnGet()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        Result = await _importer.ImportFromIdentityDbAsync(ct);
        return Page();
    }
}
