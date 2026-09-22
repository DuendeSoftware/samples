// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement.Profiles;

namespace AccountLockout.Services;

public sealed class SchemaBootstrapper(ISchemaAdmin schemaAdmin)
{
    public async Task BootstrapAsync(CancellationToken ct)
    {
        var schemaId = SchemaId.UserProfile;
        var existing = await schemaAdmin.GetAsync(schemaId, ct);

        if (!existing.Found || existing.Item is null)
        {
            var createResult = await schemaAdmin.CreateAsync(
                new SchemaConfiguration
                {
                    SchemaId = schemaId,
                    DisplayName = "User profiles",
                    Description = "Sample account profile schema for the Account Lockout sample.",
                    AttributeDefinitions = [.. AccountLockoutProfileAttributes.All.Select(CloneDefinition)]
                },
                ct);

            if (!createResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to create the user profile schema: {string.Join("; ", createResult.Errors.Select(error => error.Message))}");
            }

            return;
        }

        var configuration = existing.Item;
        var changed = false;

        foreach (var definition in AccountLockoutProfileAttributes.All)
        {
            var current = configuration.AttributeDefinitions.FirstOrDefault(candidate => candidate.Code == definition.Code);
            if (current is null)
            {
                configuration.AttributeDefinitions.Add(CloneDefinition(definition));
                changed = true;
                continue;
            }

            if (current != definition)
            {
                _ = configuration.AttributeDefinitions.Remove(current);
                configuration.AttributeDefinitions.Add(CloneDefinition(definition));
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        var updateResult = await schemaAdmin.UpdateAsync(schemaId, configuration, existing.Version, ct);
        if (!updateResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to update the user profile schema: {string.Join("; ", updateResult.Errors.Select(error => error.Message))}");
        }
    }

    private static AttributeDefinition CloneDefinition(AttributeDefinition definition) =>
        new()
        {
            Code = definition.Code,
            AttributeType = definition.AttributeType,
            Description = definition.Description,
            DisplayName = definition.DisplayName,
            IsQueryable = definition.IsQueryable,
            IsRequired = definition.IsRequired,
            IsUnique = definition.IsUnique,
            Tags = definition.Tags,
            GroupCode = definition.GroupCode,
            Order = definition.Order
        };

}
