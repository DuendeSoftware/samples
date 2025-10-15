// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Pages.Error;

[AllowAnonymous]
[SecurityHeaders]
public class Index : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly IIdentityServerInteractionService _interaction;

    public Index(IIdentityServerInteractionService interaction, IWebHostEnvironment environment)
    {
        _interaction = interaction;
        _environment = environment;
    }

    public ViewModel View { get; set; } = new();

    public async Task OnGet(string? errorId)
    {
        // retrieve error details from identityserver
        var message = await _interaction.GetErrorContextAsync(errorId);
        if (message != null)
        {
            View.Error = message;

            if (!_environment.IsDevelopment())
            {
                // only show in development
                message.ErrorDescription = null;
            }
        }
    }
}
