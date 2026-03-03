// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace IdentityServerAspNetIdentityPasskeys.Models;

public class PasskeyInputModel
{
    public string? CredentialJson { get; set; }
    public string? Error { get; set; }
}
