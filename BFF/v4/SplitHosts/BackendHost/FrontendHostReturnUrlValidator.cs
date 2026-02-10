// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Bff.Endpoints;

namespace BackendHost;

internal class FrontendHostReturnUrlValidator : IReturnUrlValidator
{
    public bool IsValidAsync(Uri returnUrl)
    {
        return returnUrl is { Host: "localhost", Port: 5011 };
    }
}
