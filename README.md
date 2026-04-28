# Kralizek.Extensions.Configuration.AWSSecretsManager

[![NuGet](https://img.shields.io/nuget/v/Kralizek.Extensions.Configuration.AWSSecretsManager.svg)](https://www.nuget.org/packages/Kralizek.Extensions.Configuration.AWSSecretsManager)
[![Build status](https://github.com/Kralizek/AWSSecretsManagerConfigurationExtensions/actions/workflows/ci.yml/badge.svg)](https://github.com/Kralizek/AWSSecretsManagerConfigurationExtensions/actions/workflows/ci.yml)

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

## Amazon Elastic Kubernetes Service (EKS)

In order to authenticate requests to AWS Secrets Manager a pod needs to use an IAM role that grants access to your secrets. Amazon introduced [IAM roles for service accounts](https://docs.aws.amazon.com/eks/latest/userguide/iam-roles-for-service-accounts.html) to make this possible without third-party solutions.

This feature requires an additional package loaded by reflection:

```bash
dotnet add package AWSSDK.SecurityToken
```

## Migration from 1.x

See [MIGRATION.md](MIGRATION.md) for the list of breaking changes.

## Mentions by the community

* [Secure secrets storage for ASP.NET Core with AWS Secrets Manager (Part 1)](https://andrewlock.net/secure-secrets-storage-for-asp-net-core-with-aws-secrets-manager-part-1/) by Andrew Lock
* [Secure secrets storage for ASP.NET Core with AWS Secrets Manager (Part 2)](https://andrewlock.net/secure-secrets-storage-for-asp-net-core-with-aws-secrets-manager-part-2/) by Andrew Lock
* [Useful tools to manage your application's secrets](https://raygun.com/blog/manage-application-secrets/) by Jerrie Pelser
* [Storing secrets CORRECTLY in .NET using AWS Secrets Manager](https://www.youtube.com/watch?v=BGW4FnEB-CM) by [Nick Chapsas](https://github.com/Elfocrash)
* [Effortless Secret Management in .NET Using AWS Secrets Manager](https://www.youtube.com/watch?v=hDVdLNJfaNU) by [Milan Jovanović](https://github.com/m-jovanovic)
* [Cloud Fundamentals: AWS Services for C# Developers](https://dometrain.com/course/cloud-fundamentals-aws-services-for-c-developers/) by [Nick Chapsas](https://github.com/Elfocrash)

## Stargazers over time

[![Stargazers over time](https://starchart.cc/Kralizek/AWSSecretsManagerConfigurationExtensions.svg)](https://starchart.cc/Kralizek/AWSSecretsManagerConfigurationExtensions)
