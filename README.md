# Kralizek.Extensions.Configuration.AWSSecretsManager

[![NuGet](https://img.shields.io/nuget/v/Kralizek.Extensions.Configuration.AWSSecretsManager.svg)](https://www.nuget.org/packages/Kralizek.Extensions.Configuration.AWSSecretsManager)
[![Build status](https://github.com/Kralizek/AWSSecretsManagerConfigurationExtensions/actions/workflows/ci.yml/badge.svg)](https://github.com/Kralizek/AWSSecretsManagerConfigurationExtensions/actions/workflows/ci.yml)

AWS Secrets Manager configuration provider for `Microsoft.Extensions.Configuration`. Load secrets directly into your .NET configuration pipeline.

## Installation

```bash
dotnet add package Kralizek.Extensions.Configuration.AWSSecretsManager
```

## Choosing a Mode

The library provides three explicit loading modes. Pick the one that matches your access pattern:

| Mode | Extension method | AWS API | Best for |
|---|---|---|---|
| **Discovery** | `AddSecretsManagerDiscovery` | `ListSecrets` + `BatchGetSecretValue` | Load all secrets in an account/region |
| **KnownSecrets** | `AddSecretsManagerKnownSecrets` | `BatchGetSecretValue` | Load a fixed set of known secrets |
| **KnownSecret** | `AddSecretsManagerKnownSecret` | `GetSecretValue` | Load exactly one secret |

---

## Discovery Mode

Discover and load all secrets returned by `ListSecrets`, then batch-fetch their values.

```csharp
// Minimal — use default AWS credentials and region
builder.AddSecretsManagerDiscovery();

// With AWSOptions (region, profile, credentials)
var awsOptions = new AWSOptions { Region = Amazon.RegionEndpoint.EUWest1 };
builder.AddSecretsManagerDiscovery(awsOptions);

// With a pre-built client
var client = new AmazonSecretsManagerClient(RegionEndpoint.EUWest1);
builder.AddSecretsManagerDiscovery(client);
```

### Filtering discovered secrets

Use `SecretFilter` to skip secrets you don't need, or `ListSecretsFilters` to apply server-side filters:

```csharp
builder.AddSecretsManagerDiscovery(options =>
{
    // Client-side filter
    options.SecretFilter = entry => entry.Name.StartsWith("myapp/");

    // Server-side filters (reduce ListSecrets results at the API level)
    options.ListSecretsFilters.Add(new Filter
    {
        Key = FilterNameStringType.Name,
        Values = new List<string> { "myapp/" }
    });
});
```

### Key customization

Control how secret names and JSON property paths become configuration keys:

```csharp
builder.AddSecretsManagerDiscovery(options =>
{
    options.KeyGenerator = (entry, key) => key.Replace("/", ":");
});
```

### Batch vs. individual fetch

Discovery mode uses `BatchGetSecretValue` by default. To fall back to one `GetSecretValue` call per secret:

```csharp
builder.AddSecretsManagerDiscovery(options =>
{
    options.UseBatchFetch = false;
});
```

---

## KnownSecrets Mode

Fetch a fixed list of secrets by ARN or name using `BatchGetSecretValue`. No `ListSecrets` call is made.

```csharp
builder.AddSecretsManagerKnownSecrets(new[]
{
    "MySecretFullARN-abcxyz",
    "MySecretPartialARN",
    "MySecretUniqueName"
});
```

With `AWSOptions` or a custom client:

```csharp
builder.AddSecretsManagerKnownSecrets(awsOptions, new[] { "my-app/db", "my-app/api-key" });
builder.AddSecretsManagerKnownSecrets(client,     new[] { "my-app/db", "my-app/api-key" });
```

---

## KnownSecret Mode

Load exactly one secret by ARN or name using `GetSecretValue`. No `ListSecrets` or batch call is made.

```csharp
builder.AddSecretsManagerKnownSecret("my-app/prod");

// With AWSOptions or a custom client
builder.AddSecretsManagerKnownSecret(awsOptions, "my-app/prod");
builder.AddSecretsManagerKnownSecret(client,     "my-app/prod");
```

---

## Multiple Registrations

You can call any combination of the three methods multiple times on the same builder. Each registration adds an independent provider that loads independently. This is useful when you need to combine different access patterns — for example, loading a shared set of secrets by discovery alongside a single high-privilege secret fetched directly:

```csharp
builder
    .AddSecretsManagerDiscovery(options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("myapp/shared/");
    })
    .AddSecretsManagerKnownSecret("myapp/high-privilege-secret")
    .AddSecretsManagerKnownSecrets(new[] { "myapp/db", "myapp/api-key" });
```

Later registrations take precedence over earlier ones when the same configuration key appears in more than one provider (standard `Microsoft.Extensions.Configuration` behaviour). Within a single provider, use `DuplicateKeyHandling` to control conflicts between secrets resolved by that provider.

---

## Duplicate Key Handling

When two secrets produce the same configuration key, control the conflict resolution:

```csharp
builder.AddSecretsManagerDiscovery(options =>
{
    options.DuplicateKeyHandling = DuplicateKeyHandling.LastWins;  // default
    // options.DuplicateKeyHandling = DuplicateKeyHandling.FirstWins;
    // options.DuplicateKeyHandling = DuplicateKeyHandling.Throw;
});
```

---

## Reload / Polling

Re-fetch secrets on a schedule:

```csharp
builder.AddSecretsManagerDiscovery(options =>
{
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```

---

## Bootstrap Logging

Attach a logger during startup before the DI container is built:

```csharp
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

builder.AddSecretsManagerDiscovery(options =>
{
    options.UseBootstrapLogging(loggerFactory);
});
```

---

## JSON Secret Flattening

Secrets whose value is a JSON object are automatically flattened into configuration key/value pairs using `:` as the separator, rooted at the secret's name.

For example, a secret named `myapp/database` with value `{"Host":"db.example.com","Port":"5432"}` produces:

```
myapp/database:Host = db.example.com
myapp/database:Port = 5432
```

---

## No Caching

The provider fetches values from AWS Secrets Manager on each `Load()` call and on every configured reload. There is no in-memory cache layer between reloads — secrets are not re-fetched until the next explicit load or scheduled reload fires.

---

## Amazon Elastic Kubernetes Service (EKS)

In order to authenticate requests to AWS Secrets Manager a pod needs to use an IAM role that grants access to your secrets. Amazon introduced [IAM roles for service accounts](https://docs.aws.amazon.com/eks/latest/userguide/iam-roles-for-service-accounts.html) to make this possible without third-party solutions.

This feature requires an additional package loaded by reflection:

```bash
dotnet add package AWSSDK.SecurityToken
```

---

## Secret Identifier Forms

How secrets are requested depends on the provider mode:

* **`KnownSecret`** — passes the configured identifier directly to `GetSecretValue`. Any value accepted by that API (secret name or full ARN) works.
* **`KnownSecrets` per-id path** (`UseBatchFetch = false`) — passes each configured identifier directly to `GetSecretValue`, same as above.
* **`KnownSecrets` batch path** (`UseBatchFetch = true`, the default) — passes configured identifiers to `BatchGetSecretValue`, then matches returned entries by exact ARN or name. A **partial ARN** (the full ARN without the random six-character suffix, e.g. `arn:aws:secretsmanager:us-east-1:123456789012:secret:my-app/prod`) is also resolved via prefix match. If a partial ARN prefix matches more than one result, `InvalidOperationException` is thrown to prevent silently loading the wrong secret.
* **`Discovery`** — uses the ARN and name returned by `ListSecrets`; callers do not provide secret ids directly.

The resolved configuration keys are always rooted at the secret's **Name** as returned by Secrets Manager, regardless of the identifier form used to request it.

---

## Secrets Scheduled for Deletion

The `ListSecrets` API may return secrets whose `DeletedDate` is set — these are secrets that have been scheduled for deletion but are still within the recovery window.

**The library does not automatically skip these entries.** It will attempt to fetch them, and depending on the secret state and AWS API behavior, the fetch may fail. Callers that use discovery should filter them out explicitly:

```csharp
builder.AddSecretsManagerDiscovery(options =>
{
    // Skip secrets scheduled for deletion.
    options.SecretFilter = entry => entry.DeletedDate == null;
});
```

---

## LocalStack / Local Development

The library does not have a built-in LocalStack integration. You can target a LocalStack endpoint by configuring the `IAmazonSecretsManager` client before registering the provider:

```csharp
var client = new AmazonSecretsManagerClient(new AmazonSecretsManagerConfig
{
    ServiceURL = "http://localhost:4566",
    AuthenticationRegion = "us-east-1"
});

builder.Configuration.AddSecretsManagerKnownSecrets(
    secretIds: new[] { "my-secret" },
    secretsManager: client);
```

For local development, prefer `KnownSecret` or `KnownSecrets` with stable secret names rather than ARNs. The `Discovery` provider fetches secrets by the ARN returned by `ListSecrets`, which LocalStack may regenerate between restarts. `KnownSecret(s)` with stable names sidestep this problem because the identifier you supply is passed directly to the API.

---



See [MIGRATION.md](MIGRATION.md) for the list of breaking changes.

---

## Mentions by the community

* [Secure secrets storage for ASP.NET Core with AWS Secrets Manager (Part 1)](https://andrewlock.net/secure-secrets-storage-for-asp-net-core-with-aws-secrets-manager-part-1/) by Andrew Lock
* [Secure secrets storage for ASP.NET Core with AWS Secrets Manager (Part 2)](https://andrewlock.net/secure-secrets-storage-for-asp-net-core-with-aws-secrets-manager-part-2/) by Andrew Lock
* [Useful tools to manage your application's secrets](https://raygun.com/blog/manage-application-secrets/) by Jerrie Pelser
* [Storing secrets CORRECTLY in .NET using AWS Secrets Manager](https://www.youtube.com/watch?v=BGW4FnEB-CM) by [Nick Chapsas](https://github.com/Elfocrash)
* [Effortless Secret Management in .NET Using AWS Secrets Manager](https://www.youtube.com/watch?v=hDVdLNJfaNU) by [Milan Jovanović](https://github.com/m-jovanovic)
* [Cloud Fundamentals: AWS Services for C# Developers](https://dometrain.com/course/cloud-fundamentals-aws-services-for-c-developers/) by [Nick Chapsas](https://github.com/Elfocrash)

## Stargazers over time

[![Stargazers over time](https://starchart.cc/Kralizek/AWSSecretsManagerConfigurationExtensions.svg)](https://starchart.cc/Kralizek/AWSSecretsManagerConfigurationExtensions)
