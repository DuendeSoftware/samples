// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.External;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Account;

public sealed class ExternalLoginModel(
    IUserAuthenticatorsSelfService authenticatorsSelfService,
    IUserProfileSelfService profileSelfService) : PageModel
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

        var authenticatorName = ExternalAuthenticatorName.Create(providerName);
        var subjectId = OpaqueSubjectId.Create(externalSub);
        var authenticator = new ExternalAuthenticator(authenticatorName, subjectId);

        var authenticators = await authenticatorsSelfService.TryGetAsync(authenticator, ct);

        UserSubjectId userId;

        if (authenticators is not null)
        {
            userId = authenticators.SubjectId;
        }
        else
        {
            var newUserId = UserSubjectId.New();

            authenticators = await authenticatorsSelfService.TryRegisterAsync(newUserId, authenticator, ct);
            if (authenticators is null)
            {
                ErrorMessage = "Could not register user.";
                return Page();
            }

            userId = authenticators.SubjectId;

            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? string.Empty;

            var schema = await profileSelfService.GetSchemaAsync(ct);
            var attributes = new AttributeValueCollection(schema);
            attributes.Set(OidcStandardAttributes.Name.Code, name);

            var profile = await profileSelfService.TryRegisterAsync(userId, attributes.Validate(), ct);
            if (profile is null)
            {
                ErrorMessage = "Could not create user profile.";
                return Page();
            }
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
