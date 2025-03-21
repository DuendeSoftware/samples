// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace Api
{
    [Route("identity")]
    public class IdentityController : ControllerBase
    {
        public IActionResult Get()
        {
            var user = User.FindFirst("name")?.Value ?? User.FindFirst("sub").Value;
            return Ok(user);
        }
    }
}
