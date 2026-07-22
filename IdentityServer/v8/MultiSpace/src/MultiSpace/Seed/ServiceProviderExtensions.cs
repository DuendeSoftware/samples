// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.MultiSpace;

namespace MultiSpace.Seed;

public static class ServiceProviderExtensions
{
    extension(IServiceProvider sp)
    {
        /// <summary>
        /// To explicitly set or override the space, you have to do this on a scoped service provider.
        /// This method will create a wrapper around such a scope for convenience.
        /// The scope will be disposed when the SpacedService is disposed.
        /// </summary>
        /// <typeparam name="T">The type to resolve with a given SpaceID associated with it. </typeparam>
        /// <param name="spaceId">The space id to set. </param>
        /// <returns>The service that's configured to use a specific space.</returns>
        public SpacedService<T> GetServiceForSpace<T>(SpaceId spaceId) where T : notnull
        {
            var scope = sp.CreateScope();
            var spacedServices = scope.ServiceProvider;
            spacedServices.GetRequiredService<ISpaceContextAccessor>().SetSpace(spaceId);
            return new SpacedService<T>(scope, spacedServices.GetRequiredService<T>());
        }

    }
}
