// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement.Profiles;

namespace AccountLockout;

public static class AccountLockoutProfileAttributes
{
    public static readonly AttributeDefinition Email = OidcStandardAttributes.Email with
    {
        IsUnique = true,
        IsQueryable = true
    };

    public static readonly AttributeDefinition Name = OidcStandardAttributes.Name with
    {
        IsQueryable = true
    };

    public static readonly AttributeCode AccountLockedUntilCode = AttributeCode.Create("account_locked_until");
    public static readonly AttributeCode AccountLockedIndefinitelyCode = AttributeCode.Create("account_locked_indefinitely");

    // These are customer-defined profile attributes. Applications can replace them
    // with a different policy model or an external SIAM/risk-system decision.
    public static readonly AttributeDefinition AccountLockedUntil = new()
    {
        Code = AccountLockedUntilCode,
        AttributeType = new ScalarAttributeType(ScalarDataType.DateTime),
        Description = AttributeDescription.Create("UTC expiry for a temporary full-account lockout."),
        DisplayName = AttributeDisplayName.Create("Account locked until"),
        IsQueryable = true
    };

    public static readonly AttributeDefinition AccountLockedIndefinitely = new()
    {
        Code = AccountLockedIndefinitelyCode,
        AttributeType = new ScalarAttributeType(ScalarDataType.Boolean),
        Description = AttributeDescription.Create("Whether the account is fully locked without an expiry."),
        DisplayName = AttributeDisplayName.Create("Account locked indefinitely"),
        IsQueryable = true
    };

    public static IReadOnlyList<AttributeDefinition> All { get; } =
    [
        Email,
        Name,
        AccountLockedUntil,
        AccountLockedIndefinitely
    ];
}
