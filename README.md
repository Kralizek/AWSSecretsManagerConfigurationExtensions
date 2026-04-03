# Kralizek.Extensions.Configuration.AWSSecretsManager

[![NuGet](https://img.shields.io/nuget/v/Kralizek.Extensions.Configuration.AWSSecretsManager.svg)](https://www.nuget.org/packages/Kralizek.Extensions.Configuration.AWSSecretsManager)

AWS Secrets Manager configuration provider for `Microsoft.Extensions.Configuration`. Load secrets directly into your .NET configuration pipeline.

## Installation

```bash
dotnet add package Kralizek.Extensions.Configuration.AWSSecretsManager
```

## Basic Usage

```csharp
var builder = new ConfigurationBuilder();

builder.AddSecretsManager();

var configuration = builder.Build();
```

## With AWSOptions

```csharp
using Amazon.Extensions.NETCore.Setup;

var awsOptions = new AWSOptions { Region = Amazon.RegionEndpoint.EUWest1 };
builder.AddSecretsManager(awsOptions);
```

## With a Direct Client

```csharp
using Amazon.SecretsManager;

var client = new AmazonSecretsManagerClient(RegionEndpoint.EUWest1);
builder.AddSecretsManager(client);
```

## Explicit Secret IDs

Instead of listing all secrets, specify exact ARNs or names:

```csharp
builder.AddSecretsManager(options =>
{
    options.SecretIds.Add("MySecretFullARN-abcxyz");
    options.SecretIds.Add("MySecretPartialARN");
    options.SecretIds.Add("MySecretUniqueName");
});
```

## Secret Filtering

Filter secrets discovered via `ListSecrets`:

```csharp
builder.AddSecretsManager(options =>
{
    options.SecretFilter = entry => entry.Name.StartsWith("myapp/");
});
```

## Key Generator

Customize how secret names become configuration keys:

```csharp
builder.AddSecretsManager(options =>
{
    options.KeyGenerator = (entry, key) => key.Replace("/", ":");
});
```

## Duplicate Key Handling

Control behaviour when two secrets produce the same configuration key:

```csharp
builder.AddSecretsManager(options =>
{
    options.DuplicateKeyHandling = DuplicateKeyHandling.LastWins;  // default
    // options.DuplicateKeyHandling = DuplicateKeyHandling.FirstWins;
    // options.DuplicateKeyHandling = DuplicateKeyHandling.Throw;
});
```

## Reload / Polling

Re-fetch secrets on a schedule:

```csharp
builder.AddSecretsManager(options =>
{
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```

## Bootstrap Logging

Attach a logger during startup before the DI container is built:

```csharp
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

builder.AddSecretsManager(options =>
{
    options.UseBootstrapLogging(loggerFactory);
});
```

## No Caching

The provider always fetches fresh values from AWS Secrets Manager when `Load()` or a reload is triggered. There is no in-memory cache layer — this is intentional so that your application always reflects the current secret state.

## Migration from 1.x

See [MIGRATION.md](MIGRATION.md) for the list of breaking changes.
