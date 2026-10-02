# Duende Storage sample

This sample runs IdentityServer with configuration and operational data stored in two separate in-memory SQLite databases through Duende Storage. It uses the IdentityServer administration APIs to create all IdentityServer configuration at startup and registers a fixed in-memory schema for client extension properties.

The sample demonstrates:

- defining a fixed client data-extension schema and its typed `allowed_region` attribute in the sample;
- creating scopes, identity resources, clients, secrets, and extension-property values with the administration APIs;
- obtaining a client-credentials token from configuration loaded through Duende Storage;
- enforcing an `allowed_region` client extension property in a custom token-request validator;
- signing in a test user with authorization code and PKCE to create a storage-backed server-side session;
- searching stored clients and inspecting stored sessions; and
- routing IdentityServer configuration data and operational data to two independently registered and migrated storage instances.

## Run the sample

The sample uses the latest prerelease packages required by this functionality:

- `Duende.IdentityServer` `8.1.0-preview.4`
- `Duende.Storage.Sqlite` `2.0.0-preview.3`

From this directory, run:

```console
dotnet run
```

Open `https://localhost:5006` if the browser does not open automatically. Both databases are held in memory and reset whenever the process stops.

## Multiple storage instances

`SampleStorage` names two Duende Storage instances: `configuration` and `operational`. `Program.cs` registers a separate in-memory SQLite database for each and routes IdentityServer's data categories to them:

```csharp
builder.Services
    .AddIdentityServer()
    // ...
    .AddStorage(SampleStorage.Configuration, storage => storage.AddSqliteInMemory("IdentityServerStorageSample-Configuration"))
    .AddStorage(SampleStorage.Operational, storage => storage.AddSqliteInMemory("IdentityServerStorageSample-Operational"))
    .AddConfigurationStorage(SampleStorage.Configuration)
    .AddOperationalStorage(SampleStorage.Operational);
```

`AddStorage` only registers a database for a named instance; it does not route any data to it. `AddConfigurationStorage`/`AddOperationalStorage` route a data category (clients, identity providers, and resources for configuration; persisted grants, server-side sessions, and keys for operational) to the instance passed to them. A data category left unmapped falls back to a default instance, but this sample maps both categories explicitly, so it registers no default instance.

Each instance is a separate database and must be migrated separately, through `IStorageInstanceSchemaFactory`:

```csharp
var schemaFactory = app.Services.GetRequiredService<IStorageInstanceSchemaFactory>();
foreach (var instance in new[] { SampleStorage.Configuration, SampleStorage.Operational })
{
    var schema = await schemaFactory.GetStorageInstanceSchema(instance, CancellationToken.None);
    await schema.MigrateAsync(CancellationToken.None);
}
```

In production, each instance would typically be a separate database and connection string, for example `AddPostgreSql()` for one instance and `AddMsSql()` for another, migrated separately and independently scaled. Each instance can use a different storage provider.

## Client credentials and the region extension

`SampleData.ClientSchema` defines the fixed schema passed to `AddInMemoryDataExtensionSchemas`. The machine and interactive clients set its typed `allowed_region` attribute through `CreateClient.ExtendedProperties`.

Request a token with the region allowed by the client's stored `allowed_region` property:

```console
curl --fail-with-body -X POST "https://localhost:5006/connect/token" -H "Content-Type: application/x-www-form-urlencoded" -H "X-Simulated-Region: eu-west" -d "grant_type=client_credentials&client_id=storage-sample-machine&client_secret=storage-sample-secret&scope=sample-api"
```

Change the header to `X-Simulated-Region: us-east` to receive an `invalid_request` response from the custom token-request validator.

The region header is caller-controlled only to make the behavior easy to exercise. Production systems must resolve region or tenancy information from a trusted source.

## User token and server-side session

1. Select **Start the authorization code flow with PKCE** on the home page.
2. Sign in with username `alice` and password `alice`.
3. The callback exchanges the authorization code and displays the token response.
4. Return to the home page and open the client search and server-side session links.

The client search uses `IClientAdmin.QueryAsync`. The session endpoint uses `IServerSideSessionStore` and shows the session persisted as operational data. Both endpoints require the sample user's `admin` claim rather than allowing any authenticated user.

The callback displays the raw token response to keep the sample focused on IdentityServer storage. A production relying party must validate tokens before using them.
