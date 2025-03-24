// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

[assembly: HostingStartup(typeof(IdentityServerAspNetIdentity.Areas.Identity.IdentityHostingStartup))]
namespace IdentityServerAspNetIdentity.Areas.Identity;

public class IdentityHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
        });
    }
}
