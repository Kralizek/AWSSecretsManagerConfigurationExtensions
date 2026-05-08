# Samples

Each sample is a minimal standalone console (or web) application that demonstrates one feature of the library. Samples are organized by intent.

## Choosing what to load

| Sample | Use case | Provider method |
|---|---|---|
| [SingleSecret](SingleSecret/Program.cs) | Load exactly one secret with least-privilege permissions | `AddSecretsManagerKnownSecret` |
| [ManySecrets](ManySecrets/Program.cs) | Load a fixed set of known secrets without calling `ListSecrets` | `AddSecretsManagerKnownSecrets` |
| [ListSecrets](ListSecrets/Program.cs) | Discover and load secrets by listing them | `AddSecretsManagerDiscovery` |

## Configuring AWS client creation

| Sample | Use case | API |
|---|---|---|
| [ConfigureAwsOptions](ConfigureAwsOptions/Program.cs) | Configure region, profile, or other AWS SDK options | `AWSOptions` |
| [UseCustomSecretsManagerClient](UseCustomSecretsManagerClient/Program.cs) | Pass a caller-created `IAmazonSecretsManager` instance | `AddSecretsManager*(client, ...)` |

## Mapping and composing configuration keys

| Sample | Use case |
|---|---|
| [MapConfigurationKeys](MapConfigurationKeys/Program.cs) | Shape configuration keys with built-in `SecretKeyMappingOptions` |
| [CustomizeConfigurationKeys](CustomizeConfigurationKeys/Program.cs) | Full key control using context-based `KeyGenerator` |
| [ResolveDuplicateKeys](ResolveDuplicateKeys/Program.cs) | Handle duplicate keys from within a single provider call |
| [ComposeConfigurationSources](ComposeConfigurationSources/Program.cs) | Layer multiple configuration sources with normal .NET precedence |

## Operational scenarios

| Sample | Use case |
|---|---|
| [BootstrapLogging](BootstrapLogging/Program.cs) | Enable logging while configuration is being built |
| [ReloadSecretsInAspNetCore](ReloadSecretsInAspNetCore/Program.cs) | Reload secrets periodically in an ASP.NET Core application |

---

## API modes at a glance

| Mode | Extension method | AWS API calls | Minimum IAM permissions |
|---|---|---|---|
| **Discovery** | `AddSecretsManagerDiscovery` | `ListSecrets` + `BatchGetSecretValue` | `secretsmanager:ListSecrets`, `secretsmanager:BatchGetSecretValue` |
| **KnownSecrets** | `AddSecretsManagerKnownSecrets` | `BatchGetSecretValue` | `secretsmanager:BatchGetSecretValue` |
| **KnownSecret** | `AddSecretsManagerKnownSecret` | `GetSecretValue` | `secretsmanager:GetSecretValue` |

> **Least-privilege tip**: If you know the exact ARN or name of every secret your application needs, use `AddSecretsManagerKnownSecret` or `AddSecretsManagerKnownSecrets`. Neither method calls `ListSecrets`, so you can omit `secretsmanager:ListSecrets` from your IAM policy entirely.

---

## Key mapping guide

### Default behavior: `SecretNamePathSeparator = "/"`

By default, path-style secret names are mapped to .NET configuration paths:

```
/my-app/production/database  →  my-app:production:database
```

### Disable path normalization for compatibility

Set `SecretNamePathSeparator = null` to restore verbatim secret-name behavior (equivalent to the 1.x default):

```csharp
options.KeyMapping.SecretNamePathSeparator = null;
```

### Load JSON secrets without the secret-name prefix

Set `PrefixJsonKeysWithSecretName = false` to strip the secret name and use only the JSON property path:

```csharp
options.KeyMapping.PrefixJsonKeysWithSecretName = false;
```

See [MapConfigurationKeys](MapConfigurationKeys/Program.cs) for a full demonstration including `TargetSection`.

### Advanced key customization

Use `KeyGenerator` only when the built-in options are not sufficient. See [CustomizeConfigurationKeys](CustomizeConfigurationKeys/Program.cs).
