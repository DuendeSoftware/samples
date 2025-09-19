# Dynamic Client Registration - CI/CD Pipeline Sample

This sample of the IdentityServer.Configuration API shows how you might use DCR in a CI/CD pipeline. In a pipeline, you might want to create short lived environments for your client applications, perhaps for a feature branch. You can register those instances of the application as their own clients using dynamic client registration. This sample shows how to do that, using a personal access token to authenticate the IdentityServer.Configuration API.

The sample contains 4 projects:

1. IdentityServer - An IdentityServer host
2. Configuration - A host for the IdentityServer.Configuration API
3. SimpleApi - A simple API that authenticates requests using bearer tokens signed by IdentityServer
4. ConsoleDcrClient - A console application that registers a client application

In this sample, we imagine that the IdentityServer host, IdentityServer.Configuration API, and supporting APIs for our project are already deployed in our environment, and we now want to provision a new client for a new instance of some client application that we are building. The first 3 projects in the list above represent the already deployed infrastructure, and the ConsoleDcrClient is a console application that you might run in your pipeline. You obtain a personal access token from IdentityServer and pass that token into your pipeline, which is configured to execute the ConsoleDcrClient.

## Running the solution

First, you need to create seed data for IdentityServer. This IdentityServer host is configured to store its configuration in a Sqlite database. Seed its configuration data by running the following commands:

```shell
cd IdentityServer
dotnet run /seed
```

Then, run the IdentityServer, Configuration, and SimpleApi projects.

Obtain a personal access token from https://localhost:5001/PAT.

Finally, run the ConsoleDcrClient, supplying the PAT using configuration or pasting it into the console window.

