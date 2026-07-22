// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;

namespace MultiSpace.Seed;

/// <summary>
/// Set of example attributes that are used during authentication
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

    public static readonly AttributeDefinition Email = new()
    {
        Code = AttributeCode.Create("Email"),
        AttributeType = new ScalarAttributeType(ScalarDataType.String),
        Description = AttributeDescription.Create("The Email of the user")
    };
}
