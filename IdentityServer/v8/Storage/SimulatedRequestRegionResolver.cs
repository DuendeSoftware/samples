// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Storage;

internal interface IRequestRegionResolver
{
    string? Resolve();
}

internal sealed class SimulatedRequestRegionResolver(IHttpContextAccessor httpContextAccessor)
    : IRequestRegionResolver
{
    public string? Resolve() =>
        httpContextAccessor.HttpContext?.Request.Headers[SampleData.RegionHeaderName].ToString();
}
