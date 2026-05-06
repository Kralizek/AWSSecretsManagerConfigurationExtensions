# Kralizek.Extensions.Configuration.AWSSecretsManager

AWS Secrets Manager configuration provider for `Microsoft.Extensions.Configuration`.

## Install

```bash
dotnet add package Kralizek.Extensions.Configuration.AWSSecretsManager
```

## Quick start

Choose the mode that matches your access pattern:

```csharp
using Microsoft.Extensions.Configuration;

// Discovery: calls ListSecrets + BatchGetSecretValue
// IAM: secretsmanager:ListSecrets, secretsmanager:BatchGetSecretValue
var configuration = new ConfigurationBuilder()
    .AddSecretsManagerDiscovery()
    .Build();

// KnownSecrets: calls BatchGetSecretValue for a fixed list — no ListSecrets
// IAM: secretsmanager:BatchGetSecretValue
var configuration = new ConfigurationBuilder()
    .AddSecretsManagerKnownSecrets(new[] { "my-app/db", "my-app/api-key" })
    .Build();

// KnownSecret: calls GetSecretValue for exactly one secret — no ListSecrets, no Batch
// IAM: secretsmanager:GetSecretValue
var configuration = new ConfigurationBuilder()
    .AddSecretsManagerKnownSecret("my-app/prod")
    .Build();
```

## Multiple registrations

Multiple calls — even across different modes — are supported and compose naturally:

```csharp
builder
    .AddSecretsManagerDiscovery(options => { options.SecretFilter = e => e.Name.StartsWith("shared/"); })
    .AddSecretsManagerKnownSecret("myapp/high-privilege-secret");
```

Later registrations take precedence over earlier ones when the same key appears in more than one provider (standard `Microsoft.Extensions.Configuration` behaviour).

## Configure with options

```csharp
var configuration = new ConfigurationBuilder()
    .AddSecretsManagerDiscovery(options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("myapp/");
        options.KeyGenerator = (entry, key) => key.Replace("/", ":");
    })
    .Build();
```

Full documentation: [GitHub repository](https://github.com/Kralizek/AWSSecretsManagerConfigurationExtensions)

## AWS SDK compatibility

Version 2.x of this library requires `AWSSDK.SecretsManager` **v4** or later.
The `GetSecretValueResponse.CreatedDate` property is a `DateTime?` in v4 and the library does not read it, so responses with or without `CreatedDate` are handled identically.

## Secret identifier forms (Discovery / KnownSecrets / KnownSecret)

All three provider modes accept any identifier form that the Secrets Manager API accepts:

* **Secret name** – e.g. `my-app/prod`
* **Full ARN** – e.g. `arn:aws:secretsmanager:us-east-1:123456789012:secret:my-app/prod-AbCdEf`
* **Partial ARN** – e.g. `arn:aws:secretsmanager:us-east-1:123456789012:secret:my-app/prod` (without the random suffix; KnownSecrets batch path only)

> **Note (KnownSecrets batch):** A partial ARN that matches more than one secret in the batch response throws `InvalidOperationException` to prevent silently loading the wrong secret. Use a more specific identifier when this occurs.

The resolved configuration keys are always rooted at the secret's **Name** as returned by Secrets Manager, regardless of the identifier form used to request it.

## Secrets scheduled for deletion

The `ListSecrets` API may return secrets whose `DeletedDate` is set — these are secrets that have been scheduled for deletion but are still within the recovery window.

**The library does not automatically skip secrets scheduled for deletion.** Attempting to fetch a secret in this state from AWS will result in a `MissingSecretValueException`.

To skip such secrets, add a filter to your discovery options:

```csharp
.AddSecretsManagerDiscovery(options =>
{
    // Skip secrets scheduled for deletion.
    options.SecretFilter = entry => entry.DeletedDate == null;
})
```

## LocalStack / local development

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

Because the library requests secrets by the identifier you supply (name, full ARN, or partial ARN), stable secret names work reliably even when LocalStack regenerates ARN suffixes between restarts.

