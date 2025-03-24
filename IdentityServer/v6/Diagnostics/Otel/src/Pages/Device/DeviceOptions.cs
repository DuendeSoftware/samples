// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


namespace Otel.Pages.Device;

public class DeviceOptions
{
    public static bool EnableOfflineAccess = true;
    public static string OfflineAccessDisplayName = "Offline Access";
    public static string OfflineAccessDescription = "Access to your applications and resources, even when you are offline";

    public static readonly string InvalidUserCode = "Invalid user code";
    public static readonly string MustChooseOneErrorMessage = "You must pick at least one permission";
    public static readonly string InvalidSelectionErrorMessage = "Invalid selection";
}
