// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement.Profiles;

namespace AccountLockout;

public static class UserProfileExtensions
{
    public static string? TryGetString(this UserProfile profile, AttributeCode code)
    {
        if (profile.Attributes.TryGetValue(code, out var value) && value.TryGetValue<string>(out var result))
        {
            return result;
        }

        return null;
    }

    public static DateTimeOffset? TryGetDateTimeOffset(this UserProfile profile, AttributeCode code)
    {
        if (profile.Attributes.TryGetValue(code, out var value) && value.TryGetValue<DateTimeOffset>(out var result))
        {
            return result;
        }

        return null;
    }

    public static bool TryGetBoolean(this UserProfile profile, AttributeCode code)
        => profile.Attributes.TryGetValue(code, out var value) && value.TryGetValue<bool>(out var result) && result;
}
