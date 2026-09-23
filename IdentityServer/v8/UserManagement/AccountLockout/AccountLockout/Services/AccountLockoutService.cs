// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Profiles;

namespace AccountLockout.Services;

public sealed class AccountLockoutService(
    IUserProfileAdmin profileAdmin,
    IUserProfileSelfService profileSelfService,
    TimeProvider timeProvider)
{
    public AccountLockoutState GetState(UserProfile profile)
    {
        // An explicit indefinite lock takes precedence if both attributes are ever present.
        if (profile.TryGetBoolean(AccountLockoutProfileAttributes.AccountLockedIndefinitelyCode))
        {
            return new AccountLockoutState(AccountLockoutKind.IndefinitelyLocked);
        }

        var lockedUntilUtc = profile.TryGetDateTimeOffset(AccountLockoutProfileAttributes.AccountLockedUntilCode)?.ToUniversalTime();
        if (lockedUntilUtc is not null && lockedUntilUtc > timeProvider.GetUtcNow())
        {
            return new AccountLockoutState(AccountLockoutKind.TemporarilyLocked, lockedUntilUtc);
        }

        // Expired timestamps are treated as unlocked without requiring a cleanup write.
        return AccountLockoutState.Unlocked;
    }

    public Task<AccountLockoutMutationResult> LockUntilAsync(UserSubjectId subjectId, DateTimeOffset lockedUntil, CancellationToken ct)
    {
        var normalized = lockedUntil.ToUniversalTime();
        if (normalized <= timeProvider.GetUtcNow())
        {
            return Task.FromResult(AccountLockoutMutationResult.Failure("The lockout expiry must be in the future."));
        }

        return UpdateLockoutAttributesAsync(
            subjectId,
            attributes =>
            {
                attributes.Set(AccountLockoutProfileAttributes.AccountLockedUntilCode, normalized);
                _ = attributes.Remove(AccountLockoutProfileAttributes.AccountLockedIndefinitelyCode);
            },
            ct);
    }

    public Task<AccountLockoutMutationResult> LockIndefinitelyAsync(UserSubjectId subjectId, CancellationToken ct) =>
        UpdateLockoutAttributesAsync(
            subjectId,
            attributes =>
            {
                attributes.Set(AccountLockoutProfileAttributes.AccountLockedIndefinitelyCode, true);
                _ = attributes.Remove(AccountLockoutProfileAttributes.AccountLockedUntilCode);
            },
            ct);

    public Task<AccountLockoutMutationResult> UnlockAsync(UserSubjectId subjectId, CancellationToken ct) =>
        UpdateLockoutAttributesAsync(
            subjectId,
            attributes =>
            {
                _ = attributes.Remove(AccountLockoutProfileAttributes.AccountLockedIndefinitelyCode);
                _ = attributes.Remove(AccountLockoutProfileAttributes.AccountLockedUntilCode);
            },
            ct);

    public string Describe(AccountLockoutState state) => state.Kind switch
    {
        AccountLockoutKind.IndefinitelyLocked => "This account is locked indefinitely.",
        AccountLockoutKind.TemporarilyLocked when state.LockedUntilUtc is not null =>
            $"This account is locked until {state.LockedUntilUtc.Value:yyyy-MM-dd HH:mm:ss 'UTC'}.",
        _ => "This account is not locked."
    };

    private async Task<AccountLockoutMutationResult> UpdateLockoutAttributesAsync(
        UserSubjectId subjectId,
        Action<AttributeValueCollection> mutate,
        CancellationToken ct)
    {
        var profile = await profileAdmin.TryGetAsync(subjectId, ct);
        if (profile is null)
        {
            return AccountLockoutMutationResult.Failure("The user profile could not be found.");
        }

        var schema = await profileSelfService.GetSchemaAsync(ct);

        // Start with every existing value so changing lockout does not discard unrelated profile data.
        var updatedAttributes = new AttributeValueCollection(schema, profile.Attributes.Values);
        mutate(updatedAttributes);

        if (!updatedAttributes.TryValidate(out var validated, out var errors))
        {
            return AccountLockoutMutationResult.Failure($"The lockout change was invalid: {string.Join(" ", errors)}");
        }

        var updatedProfile = await profileSelfService.TryUpdateAsync(subjectId, validated, ct);
        if (updatedProfile is null)
        {
            return AccountLockoutMutationResult.Failure("The lockout change could not be saved.");
        }

        return AccountLockoutMutationResult.Success();
    }
}
