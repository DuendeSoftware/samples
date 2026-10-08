// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage;

namespace Storage;

/// <summary>
/// Names the two Duende Storage instances this sample registers: one for IdentityServer
/// configuration data and one for operational data.
/// </summary>
internal static class SampleStorage
{
    internal static readonly StorageInstanceId Configuration = StorageInstanceId.Create("configuration");
    internal static readonly StorageInstanceId Operational = StorageInstanceId.Create("operational");
}
