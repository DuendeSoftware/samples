// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace IdentityServer.Pages.Device;

public class InputModel
{
    public string? Button { get; set; }
    public IEnumerable<string> ScopesConsented { get; set; } = new List<string>();
    public bool RememberConsent { get; set; } = true;
    public string? ReturnUrl { get; set; }
    public string? Description { get; set; }
    public string? UserCode { get; set; }
}
