// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using Duende.Storage.EntityAttributeValue;
using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;
using Duende.UserManagement.Import;
using Duende.UserManagement.Profiles;
using Microsoft.Data.Sqlite;
using PlatformImporter = Duende.UserManagement.Import.IUserImporter;

namespace UserManagementSample.Import;

/// <summary>
/// Imports users from an ASP.NET Identity SQLite database into the Duende Platform user store
/// using the <see cref="PlatformImporter"/> bulk import API.
/// </summary>
public sealed class AspNetIdentityImporter(
    PlatformImporter platformImporter,
    IUserProfileAdmin profileAdmin,
    string connectionString)
{
    // Namespace UUID for generating deterministic subject IDs from source user IDs
    private static readonly Guid NamespaceId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public async Task<ImportResult> ImportFromIdentityDbAsync(CancellationToken ct)
    {
        var schema = await profileAdmin.GetSchemaAsync(ct);

        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync(ct);

        // Read all users
        var users = await ReadUsersAsync(conn, ct);

        // Read all claims grouped by user
        var claimsByUser = await ReadClaimsAsync(conn, ct);

        // Map to import records
        var records = new List<UserImportRecord>();

        foreach (var user in users)
        {
            var claims = claimsByUser.GetValueOrDefault(user.Id, []);
            var record = MapUser(user, claims, schema);
            if (record is not null)
            {
                records.Add(record);
            }
        }

        if (records.Count == 0)
        {
            return ImportResult.Empty;
        }

        // Import in batches of 50
        var created = 0;
        var overwritten = 0;
        var failed = 0;
        var errors = new List<string>();
        const int batchSize = 50;

        for (var i = 0; i < records.Count; i += batchSize)
        {
            var batch = records.Skip(i).Take(batchSize).ToList();
            var result = await platformImporter.ImportAsync(batch, ct);

            created += result.CreatedCount;
            overwritten += result.UpdatedCount;
            failed += result.FailedCount;

            foreach (var r in result.Results.Where(r => r.Status == UserImportStatus.Failed))
            {
                errors.Add($"User {r.SubjectId}: {r.Error}");
            }
        }

        return new ImportResult(created, overwritten, failed, errors);
    }

    private static UserImportRecord? MapUser(
        IdentityUser user,
        IReadOnlyList<IdentityClaim> claims,
        IReadOnlyAttributeSchema schema)
    {
        // Build profile attributes using the simple scalar schema defined by this app:
        // email (string, unique), name (string), website (string), location (string)
        var attrs = new AttributeValueCollection(schema);

        // Set email from the Identity user record
        var emailCode = OidcStandardAttributes.Email.Code;
        if (!string.IsNullOrEmpty(user.Email) && schema.AttributeDefinitions.ContainsKey(emailCode))
        {
            attrs.Set(emailCode, user.Email);
        }

        // Build display name from given_name + family_name claims, or fall back to "name" claim
        var nameCode = OidcStandardAttributes.Name.Code;
        if (schema.AttributeDefinitions.ContainsKey(nameCode))
        {
            var givenName = claims.FirstOrDefault(c => c.ClaimType == "given_name")?.ClaimValue;
            var familyName = claims.FirstOrDefault(c => c.ClaimType == "family_name")?.ClaimValue;
            var nameClaim = claims.FirstOrDefault(c => c.ClaimType == "name")?.ClaimValue;

            var displayName = (givenName, familyName) switch
            {
                (not null, not null) => $"{givenName} {familyName}",
                (not null, null) => givenName,
                (null, not null) => familyName,
                _ => nameClaim
            };

            if (displayName is not null)
            {
                attrs.Set(nameCode, displayName);
            }
        }

        // Set website from claims
        var websiteCode = OidcStandardAttributes.Website.Code;
        if (schema.AttributeDefinitions.ContainsKey(websiteCode))
        {
            var websiteClaim = claims.FirstOrDefault(c => c.ClaimType == "website")?.ClaimValue;
            if (websiteClaim is not null)
            {
                attrs.Set(websiteCode, websiteClaim);
            }
        }

        // Set location from claims (map from "locale" or "address" related claims)
        var locationCode = AttributeCode.Create("location");
        if (schema.AttributeDefinitions.ContainsKey(locationCode))
        {
            var locality = claims.FirstOrDefault(c => c.ClaimType == "locality")?.ClaimValue;
            var region = claims.FirstOrDefault(c => c.ClaimType == "region")?.ClaimValue;
            var location = (locality, region) switch
            {
                (not null, not null) => $"{locality}, {region}",
                (not null, null) => locality,
                (null, not null) => region,
                _ => null
            };

            if (location is not null)
            {
                attrs.Set(locationCode, location);
            }
        }

        // Build OTP addresses
        var otpAddresses = new List<OtpAddress>();
        if (!string.IsNullOrEmpty(user.Email) && EmailAddress.TryCreate(user.Email, out var emailAddress))
        {
            otpAddresses.Add(new OtpAddress(OtpChannel.Email, emailAddress));
        }

        // Build password import
        PasswordImport? password = null;
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            password = ConvertPassword(user.PasswordHash);
        }

        return new UserImportRecord
        {
            SubjectId = UserSubjectId.Create(GenerateDeterministicGuid(user.Id).ToString()),
            ProfileAttributes = attrs.Count > 0 ? attrs.Validate() : null,
            Authenticators = new AuthenticatorImport
            {
                OtpAddresses = otpAddresses,
                Password = password
            }
        };
    }

    private static PasswordImport? ConvertPassword(string aspNetPasswordHash)
    {
        // Store the ASP.NET Identity hash blob as-is under the custom algorithm ID.
        // The AspNetIdentityPasswordHashAlgorithm handles verification, and the platform
        // automatically re-hashes to the preferred algorithm (PBKDF2-SHA512) on first login.
        try
        {
            var bytes = Convert.FromBase64String(aspNetPasswordHash);
            if (bytes.Length == 0 || bytes[0] is not (0x00 or 0x01))
            {
                return null;
            }
        }
        catch (FormatException)
        {
            return null;
        }

        return new PasswordImport(AspNetIdentityPasswordHashAlgorithm.CreateImportData(aspNetPasswordHash));
    }

#pragma warning disable CA5350 // SHA1 is used for deterministic ID generation, not security
    private static Guid GenerateDeterministicGuid(string sourceId)
    {
        var namespaceBytes = NamespaceId.ToByteArray();
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(sourceId);
        var combined = new byte[namespaceBytes.Length + sourceBytes.Length];
        namespaceBytes.CopyTo(combined, 0);
        sourceBytes.CopyTo(combined, namespaceBytes.Length);

        var hash = SHA1.HashData(combined);

        return new Guid(
            BitConverter.ToInt32(hash, 0),
            BitConverter.ToInt16(hash, 4),
            (short)((BitConverter.ToInt16(hash, 6) & 0x0FFF) | 0x4000),
            (byte)((hash[8] & 0x3F) | 0x80),
            hash[9], hash[10], hash[11], hash[12], hash[13], hash[14], hash[15]);
    }
#pragma warning restore CA5350

    private static async Task<List<IdentityUser>> ReadUsersAsync(SqliteConnection conn, CancellationToken ct)
    {
        var users = new List<IdentityUser>();
        await using var cmd = new SqliteCommand(
            "SELECT Id, UserName, Email, PasswordHash FROM AspNetUsers ORDER BY Id",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            users.Add(new IdentityUser
            {
                Id = reader.GetString(0),
                UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                PasswordHash = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return users;
    }

    private static async Task<Dictionary<string, List<IdentityClaim>>> ReadClaimsAsync(SqliteConnection conn, CancellationToken ct)
    {
        var claims = new Dictionary<string, List<IdentityClaim>>();
        await using var cmd = new SqliteCommand(
            "SELECT UserId, ClaimType, ClaimValue FROM AspNetUserClaims ORDER BY UserId",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var userId = reader.GetString(0);
            var claim = new IdentityClaim
            {
                ClaimType = reader.GetString(1),
                ClaimValue = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
            };

            if (!claims.TryGetValue(userId, out var list))
            {
                list = [];
                claims[userId] = list;
            }

            list.Add(claim);
        }

        return claims;
    }

    private sealed record IdentityUser
    {
        public required string Id { get; init; }
        public string? UserName { get; init; }
        public string? Email { get; init; }
        public string? PasswordHash { get; init; }
    }

    private sealed record IdentityClaim
    {
        public required string ClaimType { get; init; }
        public required string ClaimValue { get; init; }
    }
}
