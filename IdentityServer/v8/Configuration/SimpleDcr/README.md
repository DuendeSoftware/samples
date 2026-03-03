# Dynamic Client Registration Sample

This sample of the IdentityServer.Configuration API shows how to make simple Dynamic Client Registration (DCR) requests.

The sample contains 4 projects:

1. IdentityServer - An IdentityServer host
2. Configuration - A host for the IdentityServer.Configuration API
3. SimpleApi - A simple API that authenticates requests using bearer tokens signed by IdentityServer
4. ConsoleDcrClient - A console application that registers a client application


## Running the solution

All configuration samples use a shared IdentityServer host.
Before running your first sample, you need to create seed data for IdentityServer. This IdentityServer host is configured to store its configuration in a Sqlite database. Seed its configuration data by running the following commands:

```shell
cd IdentityServer
dotnet run /seed
```

Then, run the IdentityServer, Configuration, and SimpleApi projects.

Finally, run the ConsoleDcrClient.
