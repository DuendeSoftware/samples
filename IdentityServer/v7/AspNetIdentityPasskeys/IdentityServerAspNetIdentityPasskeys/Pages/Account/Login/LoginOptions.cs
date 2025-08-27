// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace IdentityServerAspNetIdentityPasskeys.Pages.Login;

public static class LoginOptions
{
    public static readonly bool AllowLocalLogin = true;
    public static readonly bool AllowRememberLogin = true;
    public static readonly TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);
    public static readonly string InvalidCredentialsErrorMessage = "Invalid username or password";
}
