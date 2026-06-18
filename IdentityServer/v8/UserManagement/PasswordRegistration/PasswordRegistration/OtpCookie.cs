// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;

using Duende.UserManagement;
using Duende.UserManagement.Authentication.Otp;

using Microsoft.AspNetCore.DataProtection;

namespace PasswordRegistration;

public sealed class OtpCookie(IDataProtectionProvider dataProtectionProvider, IHttpContextAccessor httpContextAccessor)
{
    private const string CookieName = "__Host-OtpAuthentication";

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("OtpAuthentication.v1");

    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext is available.");

    public void Write(OtpToken token, EmailAddress emailAddress, DateTimeOffset expiresAtUtc)
    {
        var json = JsonSerializer.Serialize(new CookieValues(token.ToString(), emailAddress.ToString()));
        var protectedJson = _protector.Protect(json);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = expiresAtUtc
        };

        HttpContext.Response.Cookies.Append(CookieName, protectedJson, cookieOptions);
    }

    public bool TryRead([NotNullWhen(true)] out OtpToken? token,
        [NotNullWhen(true)] out EmailAddress? emailAddress)
    {
        token = null;
        emailAddress = null;

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

        token = OtpToken.Create(values.Token);
        emailAddress = EmailAddress.Create(values.Email);
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

    private sealed record CookieValues(string Token, string Email);
}
