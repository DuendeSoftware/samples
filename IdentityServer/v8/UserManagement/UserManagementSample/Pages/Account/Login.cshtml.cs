using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginModel(IConfiguration configuration) : PageModel
{
    private readonly IConfiguration _configuration = configuration;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool GoogleConfigured => true;

    public void OnGet()
    {
    }
}
