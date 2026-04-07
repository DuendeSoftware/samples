// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace DPoP.Api;

public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly string _configScheme;

    public ConfigureJwtBearerOptions(string configScheme)
    {
        _configScheme = configScheme;
    }

    public void PostConfigure(string name, JwtBearerOptions options)
    {
        if (_configScheme == name)
        {
            var dpopEventsType  = typeof(DPoPJwtBearerEvents);
            if (options.EventsType != null && !dpopEventsType.IsAssignableFrom(options.EventsType))
            {
                throw new Exception("EventsType on JwtBearerOptions must derive from DPoPJwtBearerEvents to work with the DPoP support.");
            }

            if (!dpopEventsType.IsInstanceOfType(options.Events))
            {
                if (typeof(JwtBearerEvents) == options.Events.GetType())
                {
                    // Default scenario where the events type wasn't overridden?
                    options.EventsType = dpopEventsType;
                }
                else
                {
                    throw new Exception("Events on JwtBearerOptions must derive from DPoPJwtBearerEvents to work with the DPoP support.");
                }
            }
        }
    }
}
