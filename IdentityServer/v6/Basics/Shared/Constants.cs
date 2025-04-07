// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


namespace Client;

public class Urls
{
    public const string IdentityServer = "https://localhost:5001";
    public const string SampleApi = "https://localhost:5002/";

    // The API built with the OWIN pipeline defaults to not using TLS
    // so that the sample will run "out of the box", without needing
    // to create certificates and manage bindings with netsh.
    public const string SampleOwinApi = "http://localhost:5003/";
}
