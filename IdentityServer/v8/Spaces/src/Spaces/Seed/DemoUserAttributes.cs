// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;

namespace Spaces.Seed;

/// <summary>
/// Example attributes added to the built-in user profile schema, which already contains
/// email, name, given_name and family_name.
/// </summary>
public class DemoUserAttributes
{
    public static readonly AttributeDefinition UserName = new()
    {
        Code = AttributeCode.Create("UserName"),
        AttributeType = new ScalarAttributeType(ScalarDataType.String),
        Description = AttributeDescription.Create("The Username to be used in username / password auth. "),
        // Note, username has to be a unique property to be able to use it
        // for username / password authentication. 
        IsUnique = true
    };
}
