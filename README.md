![GitHub Actions status | Q42/QMS4](https://github.com/Q42/QMS4/workflows/ASP.NET%20Core%20CI/badge.svg)
# QMS4
Open source Q42 CMS  
Developer friendly, headless and modular CMS based on JsonSchema standard  
Runs on ASP.Net Core 3.0

## Features
- Headless CMS
- Support for multiple datastore plugins (CosmosDB, Azure Storage (Blob / Tables))
- JsonSchema used to describe entities
- View and edit entities
- Create (multiple) page tree's
- Paging and ordering in list view
- Support for entities in multiple languages
- Upload images and other assets
- Optional user login and admin module
- Generate JsonSchema from C# Models
- Easy installation using NuGet packages

## Installation Instructions
Install `QMS.Core` and `QMS.Storage.CosmosDB` from NuGet

Optional: `QMS.Storage.AzureStorage` 

Edit `Startup.cs` and add the following lines to `ConfigureServices`   

```cs
services.UseQms(Configuration)
  .ConfigureAzureStorage(() => new StorageConfiguration() {  ReadCmsItems = true, ReadFiles = true });
```
## Modules
QMS4 is a modular CMS and different modules are available:

### CosmosDB Data Storage
The CosmosDB module stores CmsItems to Azure CosmosDB. This module does not support storing file data. You can use the Azure Storage module for file data.
```cs
services.UseQms(Configuration)
  .ConfigureCosmosDB(() => new StorageConfiguration() { ReadCmsItems = true })
  .ConfigureAzureStorage(() => new StorageConfiguration() {  ReadFiles = true }); //Optional if you need file storage.
```

Configuration:
```json
"CosmosConfig": {
  "Endpoint": "https://localhost:8081",
  "Key": "CosmosDB-key"
}
```

### Azure Blob and Table Data Storage
Stores data in Azure Tables and file data to Azure Blob Storage.

```cs
services.UseQms(Configuration)
  .ConfigureAzureStorage(() => new StorageConfiguration() {  ReadCmsItems = true, ReadFiles = true });
```

Configuration:
```json
"AzureStorageConfig": {
  "SharedAccessSignature": "SAS Token generated in Azure Portal", //null to use development storage
  "ContainerName": "cms",
  "AssetContainerName": "cms",
  "StorageLocation" : "Tables" //Tables / Blob / Both
}
```

### Authentication
Adds user login and user list to the CMS

Add a reference to `QMS.Core.Auth` nuget package.
```cs
services.UseQms(Configuration)
  .ConfigureQmsAuth()
```

In the Configure method in Startup.cs add:
```cs
app.UseAuthentication();
app.UseMiddleware<QmsAuthenticatationMiddleware>();
```

See the example project to add a default first user to the user list.

## Interceptors
Allows you to modify the data before it's saved.
```cs
services.UseQms(Configuration)
   .AddInterceptor<ExampleInterceptor>()
```

Interceptors need to implement the interface `IWriteCmsItemInterceptor`

## Installation Instructions for Development
- Install CosmosDB emulator for Windows https://aka.ms/cosmosdb-emulator
- Install Azure Storage Emulator https://docs.microsoft.com/nl-nl/azure/storage/common/storage-use-emulator
- Optional (not needed when using emulators): Edit appsettings.json with Cosmos Endpoint and Key
- Run QMS4
- Navigate to https://localhost:44341/cms

## Dependencies
JSON Schema Editor
https://github.com/json-editor/json-editor

Azure Cosmos DB
https://github.com/Azure/azure-cosmos-dotnet-v3

NJsonSchema
https://github.com/RicoSuter/NJsonSchema

JavaScript Notifications
https://ned.im/noty/


## Roadmap
- Website SDK / Website usage example

- Searching in list view

- Multiple versions of items (with start and end time)

- Audit Trail (CosmosDB Change Feed / Blob Storage file)

## Ideas

- Configure the CMS from within the CMS

- Build a JsonSchema editor in Blazor?

- Build in JsonSchema Designer, some ideas:  
https://bjdash.github.io/JSON-Schema-Builder/  
https://jsondraft.com/4c/#tree  
https://mozilla-services.github.io/react-jsonschema-form/  
https://jsoneditoronline.org/  
