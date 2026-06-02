using System.Reflection;
using Duende.IdentityServer;
using Duende.IdentityServer.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace McpDemo.IdentityServer.Pages.Home;

[AllowAnonymous]
public class Index : PageModel
{
    public Index(LicenseInformation? license = null) => License = license;

    public string Version => typeof(Duende.IdentityServer.Hosting.IdentityServerMiddleware).Assembly
                                 .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                 ?.InformationalVersion.Split('+').First()
                             ?? "unavailable";

    public LicenseInformation? License { get; }
}
