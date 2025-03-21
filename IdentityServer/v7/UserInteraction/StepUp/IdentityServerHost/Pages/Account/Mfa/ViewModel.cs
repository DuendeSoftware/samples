// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace IdentityServerHost.Pages.Mfa;

public class ViewModel
{
    public bool MfaRequestedByClient { get; set; }
    public string ClientName { get; set; }
}
