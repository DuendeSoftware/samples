using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public LoginModel(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool ShowTestCredentials => _environment.IsDevelopment();

    public bool GoogleConfigured =>
        _configuration["Authentication:Google:ClientId"] is not (null or "not-configured");

    public void OnGet()
    {
    }
}
