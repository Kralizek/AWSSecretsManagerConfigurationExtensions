# Kralizek.Extensions.Configuration.AWSSecretsManager

AWS Secrets Manager configuration provider for `Microsoft.Extensions.Configuration`.

## Install

```bash
dotnet add package Kralizek.Extensions.Configuration.AWSSecretsManager
```

## Quick start

```csharp
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddSecretsManager()
    .Build();
```

## Configure with options

```csharp
var configuration = new ConfigurationBuilder()
    .AddSecretsManager(options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("myapp/");
        options.KeyGenerator = (entry, key) => key.Replace("/", ":");
    })
    .Build();
```

Full documentation: [GitHub repository](https://github.com/Kralizek/AWSSecretsManagerConfigurationExtensions)
