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
using MultiSpace.Seed;

namespace MultiSpace.Pages.Account;

public sealed class ExternalLoginCallbackModel(
    IExternalAuthenticator externalAuthenticator,
    IUserProfileSelfService profileSelfService) : PageModel
{
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl)
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
        var existingProfile = await profileSelfService.TryGetAsync(userId, ct);
        if (existingProfile is null)
        {
            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? "name";

            var email = principal.FindFirst(JwtClaimTypes.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value ?? "test@test.nl";

            var schema = await profileSelfService.GetSchemaAsync(ct);
            var attributes = new AttributeValueCollection(schema);
            attributes.Set(DemoUserAttributes.UserName, name);
            if (email is not null)
            {
                attributes.Set(DemoUserAttributes.Email, email);
            }

            var saved = await profileSelfService.TryCreateAsync(userId, attributes.Validate(), ct);
            if (saved is null)
            {
                ErrorMessage = "Could not create user profile.";
                return Page();
            }
        }

        await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

        // Carry forward useful claims from the external provider
        var additionalClaims = new List<Claim>
        {
            new(JwtClaimTypes.AuthenticationMethod, "external"),
            new(JwtClaimTypes.IdentityProvider, providerName)
        };

        // Forward name, email, and other standard claims from the external principal
        var claimTypesToForward = new[]
        {
            JwtClaimTypes.Name, ClaimTypes.Name,
            JwtClaimTypes.Email, ClaimTypes.Email,
            JwtClaimTypes.GivenName, ClaimTypes.GivenName,
            JwtClaimTypes.FamilyName, ClaimTypes.Surname,
            JwtClaimTypes.Picture
        };

        foreach (var claim in principal.Claims)
        {
            if (claimTypesToForward.Contains(claim.Type))
            {
                additionalClaims.Add(new Claim(claim.Type, claim.Value));
            }
        }

        var identityServerUser = new IdentityServerUser(userId.ToString())
        {
            AdditionalClaims = additionalClaims
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
