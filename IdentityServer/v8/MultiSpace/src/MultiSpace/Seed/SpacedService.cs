// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace MultiSpace.Seed;

/// <summary>
/// A convenience wrapper around a service that is configured to use a specific space.
/// Disposing this instance will dispose the underlying scope that was created to resolve the service.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="scope"></param>
/// <param name="service"></param>
public class SpacedService<T>(IServiceScope scope, T service) : IDisposable
{
    public T Service => service;
    public void Dispose() => scope.Dispose();
}
