// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountLockout;

public static class ReturnUrlHelpers
{
    public static string Normalize(string? returnUrl, IIdentityServerInteractionService interactionService, IUrlHelper url)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && (url.IsLocalUrl(returnUrl) || interactionService.IsValidReturnUrl(returnUrl)))
        {
            return returnUrl;
        }

        return url.Content("~/");
    }
}
