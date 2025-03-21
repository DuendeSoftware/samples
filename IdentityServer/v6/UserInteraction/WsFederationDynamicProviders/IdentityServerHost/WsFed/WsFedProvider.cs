// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace IdentityServerHost.WsFed;

public class WsFedProvider : IdentityProvider
{
    public WsFedProvider() : base("wsfed")
    {
    }

    public WsFedProvider(IdentityProvider other) : base("wsfed", other)
    {
    }

    public string MetadataAddress
    {
        get => this["MetadataAddress"];
        set => this["MetadataAddress"] = value;
    }
    public string RelyingPartyId
    {
        get => this["RelyingPartyId"];
        set => this["RelyingPartyId"] = value;
    }
    public bool AllowIdpInitiated
    {
        get => this["AllowIdpInitiated"] == "true";
        set => this["AllowIdpInitiated"] = value ? "true" : "false";
    }
}
