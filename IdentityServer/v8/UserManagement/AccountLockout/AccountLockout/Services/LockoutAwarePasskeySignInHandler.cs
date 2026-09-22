// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Passkeys;
using Duende.UserManagement.Profiles;

namespace AccountLockout.Services;

public sealed class LockoutAwarePasskeySignInHandler(
    IUserProfileAdmin profileAdmin,
    AccountLockoutService accountLockoutService,
    ApplicationSessionService sessionService) : IPasskeySignInHandler
{
    public async Task<IResult> SignInAsync(HttpContext context, UserAuthenticators user, bool userVerified, bool backedUp, CancellationToken ct)
    {
        var profile = await profileAdmin.TryGetAsync(user.SubjectId, ct);
        if (profile is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Account unavailable",
                detail: "The account profile could not be loaded.");
        }

        var lockoutState = accountLockoutService.GetState(profile);
        if (lockoutState.IsLocked)
        {
            // Return a real failure and deliberately do not call SignInAsync.
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Account locked",
                detail: accountLockoutService.Describe(lockoutState));
        }

        await sessionService.SignInAsync(context, profile, "passkey");
        return new PasskeyCompleteAuthenticationResult(userVerified, backedUp);
    }
}
