// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

[AllowAnonymous]
public sealed class LogoutModel : PageModel
{
    private readonly IIdentityServerInteractionService _interactionService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(IIdentityServerInteractionService interactionService, ILogger<LogoutModel> logger)
    {
        _interactionService = interactionService;
        _logger = logger;
    }

    public bool LoggedOut { get; set; }
    public string? PostLogoutRedirectUri { get; set; }
    public string? SignOutIframeUrl { get; set; }

    public async Task<IActionResult> OnGet(string logoutId)
    {
        var request = await _interactionService.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);
        if (request?.ShowSignoutPrompt == false || User.Identity?.IsAuthenticated != true)
        {
            return await OnPost(logoutId);
        }

        return Page();
    }

    public async Task<IActionResult> OnPost(string logoutId)
    {
        LoggedOut = true;

        await HttpContext.SignOutAsync();
        _logger.LogInformation("User logged out.");

        var request = await _interactionService.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);
        if (request != null)
        {
            PostLogoutRedirectUri = request.PostLogoutRedirectUri;
            SignOutIframeUrl = request.SignOutIFrameUrl;
        }

        return Page();
    }
}
