// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // Add this using

namespace ContainerizedIdentityServer.Pages.Logout;

[SecurityHeaders]
[AllowAnonymous]
public class LoggedOut : PageModel
{
    private readonly IIdentityServerInteractionService _interactionService;
    private readonly ILogger<LoggedOut> _logger; // Add logger field

    public LoggedOutViewModel View { get; set; } = default!;

    // Inject ILogger
    public LoggedOut(IIdentityServerInteractionService interactionService, ILogger<LoggedOut> logger)
    {
        _interactionService = interactionService;
        _logger = logger; // Store logger
    }

    public async Task OnGet(string? logoutId)
    {
        _logger.LogInformation("LoggedOut page OnGet called with logoutId: {LogoutId}", logoutId);

        // Log the value configured in LogoutOptions
        _logger.LogInformation("LogoutOptions.AutomaticRedirectAfterSignOut value is: {AutoRedirect}", LogoutOptions.AutomaticRedirectAfterSignOut);

        // get context information (client name, post logout redirect URI and iframe for federated signout)
        var logout = await _interactionService.GetLogoutContextAsync(logoutId);

        // Log if context was found and the redirect URI
        _logger.LogInformation("Logout Context found: {ContextFound}", logout != null);
        _logger.LogInformation("PostLogoutRedirectUri from context: {PostLogoutUri}", logout?.PostLogoutRedirectUri);

        View = new LoggedOutViewModel
        {
            // Ensure the ViewModel uses the value from LogoutOptions
            AutomaticRedirectAfterSignOut = LogoutOptions.AutomaticRedirectAfterSignOut,
            PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            ClientName = String.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
            SignOutIframeUrl = logout?.SignOutIFrameUrl
        };

        // Log the value set in the ViewModel
        _logger.LogInformation("ViewModel AutomaticRedirectAfterSignOut set to: {VmAutoRedirect}", View.AutomaticRedirectAfterSignOut);
        _logger.LogInformation("ViewModel PostLogoutRedirectUri set to: {VmPostLogoutUri}", View.PostLogoutRedirectUri);
    }
}
