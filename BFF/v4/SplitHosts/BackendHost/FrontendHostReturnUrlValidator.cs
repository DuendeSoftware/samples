// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Bff;

namespace BackendHost;

class FrontendHostReturnUrlValidator : IReturnUrlValidator
{
    public Task<bool> IsValidAsync(string returnUrl)
    {
        var uri = new Uri(returnUrl);
        return Task.FromResult(uri.Host == "localhost" && uri.Port == 5011);
    }
}
