// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Authentication.Otp;
using Duende.UserManagement.Profiles;
using Microsoft.Extensions.Options;

namespace AccountLockout.Services;

public sealed class SampleDataSeeder(
    IUserProfileAdmin profileAdmin,
    IUserProfileSelfService profileSelfService,
    IUserAuthenticatorsAdmin authenticatorsAdmin,
    IOptions<AccountLockoutSampleOptions> sampleOptions)
{
    public async Task SeedAsync(CancellationToken ct)
    {
        var options = sampleOptions.Value;
        await EnsureUserAsync(new SeededUser(options.AdminSubjectId, options.AdminEmail, options.AdminName), ct);
        await EnsureUserAsync(new SeededUser(options.UserSubjectId, options.UserEmail, options.UserName), ct);
    }

    private async Task EnsureUserAsync(SeededUser user, CancellationToken ct)
    {
        var subjectId = UserSubjectId.Create(user.SubjectId);
        var bySubject = await profileAdmin.TryGetAsync(subjectId, ct);
        var byEmail = await profileAdmin.TryGetAsync(AccountLockoutProfileAttributes.Email.Code, user.Email, ct);

        if (bySubject is null && byEmail is not null && byEmail.SubjectId != subjectId)
        {
            throw new InvalidOperationException($"A different user already owns the seeded email address '{user.Email}'.");
        }

        if (bySubject is null)
        {
            var schema = await profileAdmin.GetSchemaAsync(ct);
            var attributes = new AttributeValueCollection(schema);
            attributes.Set(AccountLockoutProfileAttributes.Email.Code, user.Email);
            attributes.Set(AccountLockoutProfileAttributes.Name.Code, user.Name);

            var created = await profileAdmin.TryAddAsync(subjectId, attributes.Validate(), ct);
            if (created is null)
            {
                throw new InvalidOperationException($"Failed to create the seeded user '{user.Email}'.");
            }
        }
        else
        {
            // Refresh only sample identity data. Existing lockout attributes must survive restarts.
            var schema = await profileSelfService.GetSchemaAsync(ct);
            var updatedAttributes = new AttributeValueCollection(schema, bySubject.Attributes.Values);
            updatedAttributes.Set(AccountLockoutProfileAttributes.Email.Code, user.Email);
            updatedAttributes.Set(AccountLockoutProfileAttributes.Name.Code, user.Name);

            if (!updatedAttributes.TryValidate(out var validated, out var errors))
            {
                throw new InvalidOperationException($"Failed to validate the seeded user '{user.Email}': {string.Join(" ", errors)}");
            }

            if (await profileSelfService.TryUpdateAsync(subjectId, validated, ct) is null)
            {
                throw new InvalidOperationException($"Failed to update the seeded user '{user.Email}'.");
            }
        }

        await EnsureOtpAddressAsync(subjectId, user.Email, ct);
    }

    private async Task EnsureOtpAddressAsync(UserSubjectId subjectId, string emailAddress, CancellationToken ct)
    {
        var otpAddress = new OtpAddress(OtpChannel.Email, EmailAddress.Create(emailAddress));
        var authenticators = await authenticatorsAdmin.TryGetAsync(subjectId, ct);

        if (authenticators is null)
        {
            if (await authenticatorsAdmin.TryAddAsync(subjectId, [otpAddress], [], ct) is null)
            {
                throw new InvalidOperationException($"Failed to create authenticators for '{emailAddress}'.");
            }

            return;
        }

        if (authenticators.OtpAddresses.Any(existing => string.Equals(existing.SubjectId.ToString(), emailAddress, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (!await authenticatorsAdmin.TryAddOtpAddressesAsync(subjectId, [otpAddress], ct))
        {
            throw new InvalidOperationException($"Failed to add the OTP address for '{emailAddress}'.");
        }
    }

    private sealed record SeededUser(string SubjectId, string Email, string Name);
}
