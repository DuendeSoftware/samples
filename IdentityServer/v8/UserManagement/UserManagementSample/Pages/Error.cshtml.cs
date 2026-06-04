// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages;

[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public sealed class ErrorModel(IIdentityServerInteractionService interaction, IWebHostEnvironment environment)
    : PageModel
{
    public ErrorMessage? Error { get; set; }

    public async Task OnGetAsync(string? errorId)
    {
        var message = await interaction.GetErrorContextAsync(errorId, HttpContext.RequestAborted);
        if (message != null)
        {
            Error = message;

            if (!environment.IsDevelopment())
            {
                // only show error description in development
                message.ErrorDescription = null;
            }
        }
    }
}
