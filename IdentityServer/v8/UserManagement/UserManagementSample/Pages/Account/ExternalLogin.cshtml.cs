// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.External;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class ExternalLoginModel(
    IExternalAuthenticator externalAuthenticator,
    IUserProfileAdmin profileAdmin) : PageModel
{
    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet(string provider, string? returnUrl)
    {
        var callbackUrl = Url.Page(
            "/Account/ExternalLogin",
            pageHandler: "Callback",
            values: new { returnUrl });

        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl
        };

        return Challenge(properties, provider);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl)
    {
        var ct = HttpContext.RequestAborted;

        var result = await HttpContext.AuthenticateAsync(
            IdentityServerConstants.ExternalCookieAuthenticationScheme);

        if (!result.Succeeded || result.Principal is null)
        {
            ErrorMessage = "External authentication failed.";
            return Page();
        }

        var principal = result.Principal;
        var providerName = result.Properties?.Items[".AuthScheme"];
        var externalSub = principal.FindFirst(JwtClaimTypes.Subject)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (providerName is null || externalSub is null)
        {
            ErrorMessage = "External authentication did not return required claims.";
            return Page();
        }

        var address = new ExternalAuthenticatorAddress(
            ExternalAuthenticatorName.Create(providerName),
            OpaqueSubjectId.Create(externalSub));

        var authResult = await externalAuthenticator.TryAuthenticateAsync(address, ct);

        if (authResult is not ExternalAuthenticationResult.Success success)
        {
            ErrorMessage = "Could not authenticate with external provider.";
            return Page();
        }

        var userId = success.UserSubjectId;

        // Ensure a profile exists for this user
        var existingProfile = await profileAdmin.TryGetAsync(userId, ct);
        if (existingProfile is null)
        {
            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? string.Empty;

            var email = principal.FindFirst(JwtClaimTypes.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;

            var schema = await profileAdmin.GetSchemaAsync(ct);
            var attributes = new AttributeValueCollection(schema);
            attributes.Set(OidcStandardAttributes.Name.Code, name);
            if (email is not null)
            {
                attributes.Set(OidcStandardAttributes.Email.Code, email);
            }

            await profileAdmin.TryAddAsync(userId, attributes.Validate(), ct);
        }

        await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

        var identityServerUser = new IdentityServerUser(userId.ToString())
        {
            AdditionalClaims =
            [
                new Claim(JwtClaimTypes.AuthenticationMethod, "external"),
                new Claim(JwtClaimTypes.IdentityProvider, providerName)
            ]
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IssuedUtc = DateTimeOffset.UtcNow,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(identityServerUser, authProperties);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/"));
    }
}
