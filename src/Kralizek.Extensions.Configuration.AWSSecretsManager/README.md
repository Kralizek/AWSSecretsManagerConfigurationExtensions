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
