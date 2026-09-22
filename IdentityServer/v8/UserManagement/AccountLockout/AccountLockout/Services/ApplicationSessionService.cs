// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Authentication;

namespace AccountLockout.Services;

public sealed class ApplicationSessionService(
    IUserProfileSelfService profileSelfService,
    TimeProvider timeProvider)
{
    public async Task<UserProfile?> EnsureProfileAsync(UserSubjectId subjectId, string emailAddress, CancellationToken ct)
    {
        var existingProfile = await profileSelfService.TryGetAsync(subjectId, ct);
        if (existingProfile is not null)
        {
            return existingProfile;
        }

        var schema = await profileSelfService.GetSchemaAsync(ct);
        var attributes = new AttributeValueCollection(schema);
        attributes.Set(AccountLockoutProfileAttributes.Email.Code, emailAddress);
        attributes.Set(AccountLockoutProfileAttributes.Name.Code, emailAddress);
        return await profileSelfService.TryCreateAsync(subjectId, attributes.Validate(), ct);
    }

    public async Task SignInAsync(HttpContext httpContext, UserProfile profile, string authenticationMethod)
    {
        var now = timeProvider.GetUtcNow();
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.AuthenticationMethod, authenticationMethod)
        };

        var email = profile.TryGetString(AccountLockoutProfileAttributes.Email.Code);
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(JwtClaimTypes.Email, email));
        }

        var name = profile.TryGetString(AccountLockoutProfileAttributes.Name.Code);
        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(JwtClaimTypes.Name, name));
        }

        var user = new IdentityServerUser(profile.SubjectId.ToString())
        {
            DisplayName = name ?? email ?? profile.SubjectId.ToString(),
            AuthenticationTime = now.UtcDateTime,
            AdditionalClaims = claims
        };

        var properties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IssuedUtc = now,
            ExpiresUtc = now.AddHours(8),
            IsPersistent = true
        };

        await httpContext.SignInAsync(user, properties);
    }
}
