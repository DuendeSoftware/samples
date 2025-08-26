// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace IdentityServerAspNetIdentityPasskeys.Pages.Device;

public static class DeviceOptions
{
    public static readonly bool EnableOfflineAccess = true;
    public static readonly string OfflineAccessDisplayName = "Offline Access";

    public static readonly string OfflineAccessDescription =
        "Access to your applications and resources, even when you are offline";

    public static readonly string InvalidUserCode = "Invalid user code";
    public static readonly string MustChooseOneErrorMessage = "You must pick at least one permission";
    public static readonly string InvalidSelectionErrorMessage = "Invalid selection";
}
