// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ApiHost;

public class DPoPProofValidatonContext
{
    /// <summary>
    /// The ASP.NET Core authentication scheme triggering the validation
    /// </summary>
    public string Scheme { get; set; }

    /// <summary>
    /// The HTTP URL to validate
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// The HTTP method to validate
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// The DPoP proof token to validate
    /// </summary>
    public string ProofToken { get; set; }

    /// <summary>
    /// The access token
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// The claims associated with the access token. 
    /// </summary>
    public IEnumerable<Claim> AccessTokenClaims { get; set; } = Enumerable.Empty<Claim>();
}
