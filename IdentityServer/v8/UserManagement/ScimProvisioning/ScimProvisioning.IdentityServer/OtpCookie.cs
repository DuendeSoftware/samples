// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ScimProvisioning.IdentityServer;

public sealed class OtpCookie(IDataProtectionProvider protection, IHttpContextAccessor contextAccessor)
{
    private const string Name = "__Host-ScimProvisioningOtp";
    private readonly ITimeLimitedDataProtector _protector = protection
        .CreateProtector("ScimProvisioning.Otp.v1").ToTimeLimitedDataProtector();

    private HttpContext Context => contextAccessor.HttpContext
        ?? throw new InvalidOperationException("An HTTP context is required.");

    public void Write(State state, DateTimeOffset expiresAt) =>
        Context.Response.Cookies.Append(Name, _protector.Protect(JsonSerializer.Serialize(state), expiresAt),
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = expiresAt, Path = "/" });

    public State? Read()
    {
        if (!Context.Request.Cookies.TryGetValue(Name, out var value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<State>(_protector.Unprotect(value, out _));
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Clear() => Context.Response.Cookies.Delete(Name,
        new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/" });

    public sealed record State(string Token, string Email, string SubjectId);
}
