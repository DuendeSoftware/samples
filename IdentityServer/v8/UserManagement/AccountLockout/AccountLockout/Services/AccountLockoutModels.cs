// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace AccountLockout.Services;

public enum AccountLockoutKind
{
    Unlocked,
    TemporarilyLocked,
    IndefinitelyLocked
}

public sealed record AccountLockoutState(AccountLockoutKind Kind, DateTimeOffset? LockedUntilUtc = null)
{
    public bool IsLocked => Kind is not AccountLockoutKind.Unlocked;

    public static AccountLockoutState Unlocked { get; } = new(AccountLockoutKind.Unlocked);
}

public sealed record AccountLockoutMutationResult(
    bool Succeeded,
    string? ErrorMessage)
{
    public static AccountLockoutMutationResult Success() =>
        new(true, null);

    public static AccountLockoutMutationResult Failure(string errorMessage) =>
        new(false, errorMessage);
}
