# Migration Guide

## 2.0.x — Built-in key mapping (SecretNamePathSeparator default change)

### Default key generation now normalizes `/` in secret names

The introduction of `SecretKeyMappingOptions` changed the default configuration key generation for secrets whose names contain `/`.

**Before** (2.0.0):

A secret named `/my-app/production/database` produced configuration keys like:

```
/my-app/production/database          (scalar)
/my-app/production/database:Key      (JSON)
```

**After** (2.0.1+):

The default `SecretNamePathSeparator = "/"` replaces each `/` in the secret-name portion with the
.NET configuration path delimiter (`:`), trimming any leading/trailing delimiters:

```
my-app:production:database           (scalar)
my-app:production:database:Key       (JSON)
```

#### To restore the previous (verbatim) behavior

Set `SecretNamePathSeparator = null` on the options:

```csharp
builder.Configuration.AddSecretsManagerKnownSecret(
    "my-app/production",
    options =>
    {
        options.KeyMapping.SecretNamePathSeparator = null;
    });

builder.Configuration.AddSecretsManagerKnownSecrets(
    new[] { "/my-app/production", "/my-app/shared" },
    options =>
    {
        options.KeyMapping.SecretNamePathSeparator = null;
    });

builder.Configuration.AddSecretsManagerDiscovery(
    options =>
    {
        options.KeyMapping.SecretNamePathSeparator = null;
    });
```

#### Custom `KeyGenerator` that relied on `/` in `DefaultKey`

If you used a `KeyGenerator` that parsed or stripped `/` from `context.DefaultKey`, update it to
work with the already-normalized (colon-separated) `DefaultKey`, or disable normalization with
`SecretNamePathSeparator = null` and keep the existing logic unchanged.

---

## 1.x → 2.0

Version 2.0 replaces the single `AddSecretsManager` API with three explicit, purpose-built methods. This is a **breaking change**: all call sites must be updated.

### `AddSecretsManager` removed

`AddSecretsManager` has been **removed**. Choose the method that matches your use case:

#### Discovery — load all secrets via `ListSecrets`

```csharp
// Before
builder.AddSecretsManager();
builder.AddSecretsManager(awsOptions);
builder.AddSecretsManager(client);
builder.AddSecretsManager(options => { options.SecretFilter = ...; });

// After
builder.AddSecretsManagerDiscovery();
builder.AddSecretsManagerDiscovery(awsOptions);
builder.AddSecretsManagerDiscovery(client);
builder.AddSecretsManagerDiscovery(options => { options.SecretFilter = ...; });
```

#### KnownSecrets — load a fixed set of secrets by ARN/name

```csharp
// Before
builder.AddSecretsManager(options =>
{
    options.AcceptedSecretArns = new List<string> { "my-secret-1", "my-secret-2" };
});

// After
builder.AddSecretsManagerKnownSecrets(new[] { "my-secret-1", "my-secret-2" });
```

#### KnownSecret — load exactly one secret

```csharp
// Before
builder.AddSecretsManager(options =>
{
    options.AcceptedSecretArns = new List<string> { "my-app/prod" };
});

// After
builder.AddSecretsManagerKnownSecret("my-app/prod");
```

### Options class replaced

`SecretsManagerConfigurationProviderOptions` has been replaced by three separate options classes:

| Old | New |
|---|---|
| `SecretsManagerConfigurationProviderOptions` (with `SecretFilter`, `KeyGenerator`, etc.) | `SecretsManagerDiscoveryOptions` |
| `SecretsManagerConfigurationProviderOptions` (with `AcceptedSecretArns`) | `SecretsManagerKnownSecretsOptions` |
| `SecretsManagerConfigurationProviderOptions` (with a single `AcceptedSecretArns` entry) | `SecretsManagerKnownSecretOptions` |

Options are configured via the lambda overloads of each method:

```csharp
// Before
builder.AddSecretsManager(options =>
{
    options.KeyGenerator = context => context.DefaultKey.ToUpper();
    options.PollingInterval = TimeSpan.FromMinutes(5);
});

// After
builder.AddSecretsManagerDiscovery(options =>
{
    options.KeyGenerator = context => context.DefaultKey.ToUpper();
    options.ReloadInterval = TimeSpan.FromMinutes(5);
});
```

### 2.0 beta: `KeyGenerator` now receives a context object

During the 2.0 beta cycle, `KeyGenerator` was updated from a two-parameter delegate to a context-based delegate:

```csharp
// Before beta adjustment
options.KeyGenerator = (entry, key) => key;

// Current beta API
options.KeyGenerator = context => context.DefaultKey;
```

Use `SecretKeyGeneratorContext` to inspect:
- `SecretId`, `SecretName`, `SecretArn`
- `RawKey`, `DefaultKey`
- `JsonPath` / `HasJsonPath` (for JSON-derived keys)

### `IgnoreMissingValues` removed

`SecretsManagerConfigurationProviderOptions.IgnoreMissingValues` has been removed with no replacement. All three providers now throw `MissingSecretValueException` when a secret cannot be found.

### `AcceptedSecretArns` removed

`SecretsManagerConfigurationProviderOptions.AcceptedSecretArns` has been removed. Use `AddSecretsManagerKnownSecret` or `AddSecretsManagerKnownSecrets` instead (see above).

### `PollingInterval` → `ReloadInterval`

```csharp
// Before
options.PollingInterval = TimeSpan.FromMinutes(5);

// After (on any of the three options classes)
options.ReloadInterval = TimeSpan.FromMinutes(5);
```

### `SecretsManagerConfigurationSource` is now `internal`

`SecretsManagerConfigurationSource` was `public` in 1.x. It is now `internal`. Any code that referenced this type directly will no longer compile — use the extension methods instead.

```csharp
// Before (1.x) — no longer compiles
var source = new SecretsManagerConfigurationSource(options);
builder.Add(source);

// After
builder.AddSecretsManagerDiscovery(options => { ... });
```

### Credentials / region overload removed

The old overload accepting `AWSCredentials?` and `RegionEndpoint?` has been removed:

```csharp
// Before (removed)
builder.AddSecretsManager(credentials, region, configurator);
```

Use `AWSOptions` or pass a pre-built `IAmazonSecretsManager` client instead:

```csharp
var awsOptions = builder.Configuration.GetAWSOptions();
builder.AddSecretsManagerDiscovery(awsOptions, options => { ... });

// Or with a pre-built client
var config = new AmazonSecretsManagerConfig { ServiceURL = "http://localhost:4566" };
var client = new AmazonSecretsManagerClient(credentials, config);
builder.AddSecretsManagerDiscovery(client, options => { ... });
```

### Note for 2.0 preview users (`SecretIds` on options)

If you tried an early 2.0 beta build that exposed a `SecretIds` property on the options class, that property has been removed. Use the dedicated methods instead:

```csharp
// Early 2.0 preview (no longer supported)
builder.AddSecretsManager(options =>
{
    options.SecretIds = new[] { "my-secret-1", "my-secret-2" };
});

// Current API
builder.AddSecretsManagerKnownSecrets(new[] { "my-secret-1", "my-secret-2" });
// or, for a single secret:
builder.AddSecretsManagerKnownSecret("my-secret-1");
```

### `CreateClient` and `ConfigureSecretsManagerConfig` hooks removed

The `CreateClient` factory and `ConfigureSecretsManagerConfig` callback have been removed. Use the `IAmazonSecretsManager` overload to pass a fully-configured client:

```csharp
// Before
builder.AddSecretsManager(options =>
{
    options.ConfigureSecretsManagerConfig = config =>
    {
        config.ServiceURL = "http://localhost:4566";
        config.Timeout = TimeSpan.FromSeconds(5);
    };
});

// After
var config = new AmazonSecretsManagerConfig
{
    ServiceURL = "http://localhost:4566",
    Timeout = TimeSpan.FromSeconds(5)
};
var client = new AmazonSecretsManagerClient(credentials, config);
builder.AddSecretsManagerDiscovery(client, options => { ... });
```

### Public namespace for exception and context types

`MissingSecretValueException` and `SecretValueContext` have been moved from `Kralizek.Extensions.Configuration.Internal` to `Kralizek.Extensions.Configuration`.

```csharp
// Before
using Kralizek.Extensions.Configuration.Internal;

// After
using Kralizek.Extensions.Configuration;
```

### Newtonsoft.Json removed

The package no longer depends on Newtonsoft.Json. JSON parsing is now done with `System.Text.Json`.

### `DuplicateKeyHandling` enum added (behavior change)

`DuplicateKeyHandling` controls what happens when two secrets produce the same configuration key. The default is `LastWins`.

```csharp
options.DuplicateKeyHandling = DuplicateKeyHandling.LastWins;  // default
options.DuplicateKeyHandling = DuplicateKeyHandling.FirstWins;
options.DuplicateKeyHandling = DuplicateKeyHandling.Throw;
```

### New logging infrastructure

A structured logging pipeline has been added:

- `SecretsManagerLogEvent` — represents a single log entry
- `SecretsManagerLogEvents` — well-known `EventId` constants
- `options.UseLogging(ILogger)` — convenience extension
- `options.UseBootstrapLogging(ILoggerFactory)` — bootstrap logging before DI is ready

### `ListSecretsFilters` added

`SecretsManagerDiscoveryOptions` exposes a `ListSecretsFilters` property for applying server-side filters to the `ListSecrets` call. Use `.Add()` to populate it:

```csharp
options.ListSecretsFilters.Add(new Filter { Key = FilterNameStringType.Name, Values = new List<string> { "myapp/" } });
```
