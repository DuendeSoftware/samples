# Dynamic Client Registration - Software Statement Sample

This sample of the IdentityServer.Configuration API shows how you might use a software statement to pass client metadata values used in Dynamic Client Registration (DCR).

The sample contains 4 projects:

1. IdentityServer - An IdentityServer host
2. Configuration - A host for the IdentityServer.Configuration API
3. SimpleApi - A simple API that authenticates requests using bearer tokens signed by IdentityServer
4. ConsoleDcrClient - A console application that registers a client application

A software statement is a JWT that contains client metadata passed in a DCR request. The DCR request can set metadata either directly in the body of the request, in the software statement, or mix the two together. 

To use a software statement with the IdentityServer.Configuration API, you need to add a customized `IDynamicClientRegistrationValidator` that will verify the software statement and then make use of the claims within the statement. The easiest way to do that is to extend the default `DynamicClientRegistrationValidator` and override its `ValidateSoftwareStatementAsync` method. This method needs to read the incoming software statement, check its validity, and then copy the claims within the statement into the request object so that they will be used by the rest of the validator.

The software statement is signed with an RSA key, so the caller needs some mechanism of producing signed tokens, and the configuration API needs some mechanism of acquiring the public keys needed to verify the signatures of the tokens. The OAuth and OIDC specs on DCR specifically don't mandate a particular mechanism for distributing those keys or establishing those trust relationships. IdentityServer.Configuration allows you to add customization to implement this however you like. In this sample, we have hard-coded an RSA private key into ConsoleDcrClient and the corresponding public key material into the Configuration API.

## Running the solution

First, you need to create seed data for IdentityServer. This IdentityServer host is configured to store its configuration in a Sqlite database. Seed its configuration data by running the following commands:

```shell
cd IdentityServer
dotnet run /seed
```

Then, start the IdentityServer, Configuration, and SimpleApi projects.

Finally, run the ConsoleDcrClient.
