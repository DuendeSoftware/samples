// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.UserManagement;

namespace Spaces.Services;

public sealed class SpaceClaimAugmentationProfileService(UserManagementProfileService inner) : IProfileService
{
    // Claim type constant
    // "space_id" is a custom claim type.  In a production deployment you would
    // register it as a UserClaim on the relevant IdentityResource or ApiScope so
    // that OIDC clients can request it by name.  For this demo sample we emit it
    // unconditionally (see comment on GetProfileDataAsync below).
    public const string SpaceIdClaimType = "space";

    // The UserManagement UserManagementProfileService already handles standard OIDC claims
    // (email, name, sub, etc.) for UserManagement-backed users.  We delegate to
    // it first so all standard claims are populated, then we add our space_id.
    //
    // We depend on the concrete UserManagementProfileService rather than IProfileService
    // to avoid a circular dependency: AddProfileService<SpaceClaimAugmentationProfileService>
    // replaces the IProfileService registration, so resolving IProfileService here
    // would result in infinite recursion.  Using the concrete type bypasses the
    // decorator chain safely.

    /// <inheritdoc />
    public async Task GetProfileDataAsync(
        ProfileDataRequestContext context,
        CancellationToken cancellationToken)
    {
        // Delegate to the wrapped service (e.g. UserManagement's ProfileService)
        // so standard OIDC claims are populated first.
        await inner.GetProfileDataAsync(context, cancellationToken);

        // Forward the space_id claim that was embedded into the session cookie at
        // login time (see Pages/Account/Login/Index.cshtml.cs).
        // We emit it unconditionally (not gated on RequestedClaimTypes) so that
        // the API resource receiving access_tokens always has the space context
        // without requiring per-client claim-request configuration.
        var spaceIdClaim = context.Subject.FindFirst(SpaceIdClaimType);
        if (spaceIdClaim is not null &&
            !context.IssuedClaims.Any(c => c.Type == SpaceIdClaimType))
        {
            context.IssuedClaims.Add(spaceIdClaim);
        }
    }

    /// <inheritdoc />
    public Task IsActiveAsync(IsActiveContext context, CancellationToken cancellationToken)
        => inner.IsActiveAsync(context, cancellationToken);
}
