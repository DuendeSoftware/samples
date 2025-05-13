// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Text;
using System.Text.Json;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;

namespace SessionMigration.Pages.Diagnostics;

public class ViewModel
{
    public ViewModel(AuthenticateResult result)
    {
        AuthenticateResult = result;

        if (result.Properties.Items.ContainsKey("client_list"))
        {
            var encoded = result.Properties.Items["client_list"];
            var bytes = Base64Url.Decode(encoded);
            var value = Encoding.UTF8.GetString(bytes);

            Clients = JsonSerializer.Deserialize<string[]>(value);
        }
    }

    public AuthenticateResult AuthenticateResult { get; }
    public IEnumerable<string> Clients { get; } = new List<string>();
}
