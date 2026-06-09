// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.UserManagement.Authentication.Otp;

namespace UserManagementSample.GettingStarted;

public class ConsoleOtpDispatcher : IOtpDispatcher
{
    public bool CanDispatch(OtpAddress address) => true;

    public Task DispatchAsync(OtpAddress address, PlainTextOtp otp, TimeSpan expiresAfter, CancellationToken ct)
    {
        Console.WriteLine($"OTP for {address}: {otp.Text}");
        return Task.CompletedTask;
    }
}
