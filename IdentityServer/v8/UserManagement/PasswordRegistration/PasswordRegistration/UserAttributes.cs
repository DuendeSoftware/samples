// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement.Profiles;

namespace PasswordRegistration;

internal static class UserAttributes
{
    /// <summary>
    /// The attribute to track a unique email for a user
    /// Only one entry of the email is allowed in the system (IsUnique = true)
    /// This also allows tying the sign-in to the email, to allow email/password sign-in
    /// </summary>
    internal static readonly AttributeDefinition Email = new()
    {
        IsUnique = true,
        Code = AttributeCode.Create("email"),
        AttributeType = new ScalarAttributeType(ScalarDataType.String),
        Description = AttributeDescription.Create("End-User primary e-mail address.")
    };

    internal static readonly AttributeDefinition Name = new()
    {
        Code = "name",
        AttributeType = new ScalarAttributeType(ScalarDataType.String),
        Description = "Your full name in displayable form including all name parts, titles and suffixes, ordered according to your locale and preferences."
    };

    internal static readonly AttributeDefinition FavoriteDinosaur = new()
    {
        Code = "favorite_dinosaur",
        AttributeType = new ScalarAttributeType(ScalarDataType.String),
        Description = "Your favorite dinosaur. This is used for fun and personalization purposes."
    };

    internal static async Task BootstrapAsync(this IUserProfileSchemaAdmin admin, CancellationToken ct)
    {
        var definitions = await admin.GetAllAttributeDefinitionsAsync(ct);
        await definitions.EnsureContainsAsync(Name, admin, ct);
        await definitions.EnsureContainsAsync(FavoriteDinosaur, admin, ct);
        await definitions.EnsureContainsAsync(Email, admin, ct);
    }

    private static async Task EnsureContainsAsync(
        this IReadOnlyDictionary<AttributeCode, AttributeDefinition> definitions,
        AttributeDefinition definition, IUserProfileSchemaAdmin admin, CancellationToken ct)
    {
        if (!definitions.ContainsKey(definition.Code))
        {
            if (!await admin.TryAddAttributeDefinitionAsync(definition, ct))
            {
                throw new InvalidOperationException($"Could not add user schema attribute '{definition.Code}'.");
            }
        }
    }
}
