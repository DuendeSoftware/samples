using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Aspire.Web.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            var props = new AuthenticationProperties()
            {
                RedirectUri = "/"
            };

            return SignOut(
                props,
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
