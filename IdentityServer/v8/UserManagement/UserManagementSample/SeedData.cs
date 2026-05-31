// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.Storage.Querying;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Otp;
using Duende.UserManagement.Authentication.Passwords;
using Duende.UserManagement.Import;
using Duende.UserManagement.Profiles;
using Duende.UserManagement;

namespace UserManagementSample;

internal static class SeedData
{
    private const string BobEmail = "bob@duendesoftware.com";
    private const string AliceEmail = "alice@duendesoftware.com";
    private const string AlicePassword = "BadPassword123!#";

    private static readonly AttributeCode EmailCode = OidcStandardAttributes.Email.Code;
    private static readonly AttributeCode NameCode = OidcStandardAttributes.Name.Code;
    private static readonly AttributeCode WebsiteCode = OidcStandardAttributes.Website.Code;
    private static readonly AttributeCode LocationCode = AttributeCode.Create("location");

    private static readonly AttributeDefinition LocationAttribute = new()
    {
        Code = LocationCode,
        AttributeType = new ScalarAttributeType(ScalarDataType.String),
        Description = AttributeDescription.Create("The user's location.")
    };

    public static void EnsureSeedData(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    private static async Task SeedAsync(IServiceProvider services)
    {


        var profileAdmin = services.GetRequiredService<IUserProfileAdmin>();
        var schemaAdmin = services.GetRequiredService<IUserProfileSchemaAdmin>();
        var authenticatorsAdmin = services.GetRequiredService<IUserAuthenticatorsAdmin>();
        var importer = services.GetRequiredService<IUserImporter>();

        // Get the built-in passowrd algorighm.
        // todo: replace with PasswordHashAlgorithms.Preferred
        var passwordHashAlgorithm = services.GetServices<IPasswordHashAlgorithm>().First();

        // Ensure required attribute definitions exist in the schema
        await EnsureAttributeAsync(schemaAdmin, OidcStandardAttributes.Email with
        {
            IsUnique = true
        });
        await EnsureAttributeAsync(schemaAdmin, OidcStandardAttributes.Name);
        await EnsureAttributeAsync(schemaAdmin, OidcStandardAttributes.Website);
        await EnsureAttributeAsync(schemaAdmin, LocationAttribute);

        await SeedBobAsync(profileAdmin, authenticatorsAdmin);
        await SeedAliceAsync(profileAdmin, importer, passwordHashAlgorithm);
    }

    private static async Task SeedBobAsync(
        IUserProfileAdmin profileAdmin,
        IUserAuthenticatorsAdmin authenticatorsAdmin)
    {
        // Idempotency check — skip if Bob already exists
        var existing = await profileAdmin.TryGetAsync(EmailCode, BobEmail, default);
        if (existing is not null)
        {
            return;
        }

        var schema = await profileAdmin.GetSchemaAsync(default);
        var attributes = new AttributeValueCollection(schema);

        attributes.Set(EmailCode, BobEmail);
        attributes.Set(NameCode, "Bob Smith");
        attributes.Set(LocationCode, "somewhere");

        var profile = await profileAdmin.TryAddAsync(UserSubjectId.New(), attributes.Validate(), default);
        if (profile is null)
        {
            throw new InvalidOperationException("Failed to create Bob's user profile.");
        }

        var otpAddress = new OtpAddress(OtpChannel.Email, EmailAddress.Create(BobEmail));
        await authenticatorsAdmin.TryAddAsync(profile.SubjectId, [otpAddress], [], default);
    }

    private static async Task SeedAliceAsync(
        IUserProfileAdmin profileAdmin,
        IUserImporter importer,
        IPasswordHashAlgorithm passwordHashAlgorithm)
    {

        // Idempotency check — skip if Alice already exists
        var existing = await profileAdmin.TryGetAsync(EmailCode, AliceEmail, default);
        if (existing is not null)
        {
            return;
        }

        var schema = await profileAdmin.GetSchemaAsync(default);
        var attributes = new AttributeValueCollection(schema);

        attributes.Set(EmailCode, AliceEmail);
        attributes.Set(NameCode, "Alice Smith");
        attributes.Set(WebsiteCode, "http://alice.example");

        var hashedPassword = passwordHashAlgorithm.Hash(AlicePassword);
        var record = new UserImportRecord
        {
            SubjectId = UserSubjectId.New(),
            UserName = AliceEmail,
            ProfileAttributes = attributes.Validate(),
            Authenticators = new AuthenticatorImport
            {
                Password = new PasswordImport(hashedPassword)
            }
        };

        var result = await importer.ImportAsync([record], default);
        if (result.FailedCount > 0)
        {
            throw new InvalidOperationException("Failed to create Alice's user profile.");
        }
    }

    private static async Task EnsureAttributeAsync(IUserProfileSchemaAdmin schemaAdmin, AttributeDefinition definition)
    {
        var existing = await schemaAdmin.GetAllAttributeDefinitionsAsync(default);
        if (!existing.ContainsKey(definition.Code))
        {
            await schemaAdmin.TryAddAttributeDefinitionAsync(definition, default);
        }
    }
}
