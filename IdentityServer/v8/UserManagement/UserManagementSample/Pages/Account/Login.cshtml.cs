using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool GoogleConfigured =>
        _configuration["Authentication:Google:ClientId"] is not (null or "not-configured");

    public void OnGet()
    {
    }
}
