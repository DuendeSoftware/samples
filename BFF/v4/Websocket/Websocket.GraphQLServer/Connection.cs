// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

public record Connection : IDisposable
{
    public required Task CancelTask { get; init; }
    public required CancellationTokenSource TokenSource { get; init; }

    public void Dispose()
    {
        CancelTask.Dispose();
        TokenSource.Dispose();
    }
}
