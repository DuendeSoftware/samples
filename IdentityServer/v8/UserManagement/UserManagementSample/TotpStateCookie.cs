// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using Duende.UserManagement;
using Microsoft.AspNetCore.DataProtection;

namespace UserManagementSample;

/// <summary>
/// Stores the interim authenticated subject ID between the password step and the
/// TOTP step in a two-factor authentication flow.
/// </summary>
public sealed class TotpStateCookie(IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor)
{
    private const string CookieName = "__Host-TotpState";
    private const int CookieLifetimeMinutes = 5;

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("TotpState.v1");

    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext is available.");

    public void Write(UserSubjectId subjectId)
    {
        var json = JsonSerializer.Serialize(new CookieValues(subjectId.ToString()));
        var protectedJson = _protector.Protect(json);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(CookieLifetimeMinutes)
        };

        HttpContext.Response.Cookies.Append(CookieName, protectedJson, cookieOptions);
    }

    public bool TryRead([NotNullWhen(true)] out UserSubjectId? subjectId)
    {
        subjectId = null;

        if (!HttpContext.Request.Cookies.TryGetValue(CookieName, out var protectedJson) ||
            string.IsNullOrWhiteSpace(protectedJson))
        {
            return false;
        }

        string json;
        try
        {
            json = _protector.Unprotect(protectedJson);
        }
        catch (CryptographicException)
        {
            return false;
        }

        CookieValues? values;
        try
        {
            values = JsonSerializer.Deserialize<CookieValues>(json);
        }
        catch (JsonException)
        {
            return false;
        }

        if (values is null)
        {
            return false;
        }

        subjectId = UserSubjectId.Create(values.SubjectId);
        return true;
    }

    public void Clear() =>
        HttpContext.Response.Cookies.Delete(
            CookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true
            });

    private sealed record CookieValues(string SubjectId);
}
