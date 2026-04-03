# Migration Guide: 1.x → 2.0

This document lists all breaking changes introduced in version 2.0.

## Options class renamed

`SecretsManagerConfigurationProviderOptions` has been **removed**. Use `SecretsManagerOptions` instead.

```csharp
// Before
var options = new SecretsManagerConfigurationProviderOptions();

// After
var options = new SecretsManagerOptions();
```

## `AcceptedSecretArns` → `SecretIds`

The `AcceptedSecretArns` property has been renamed to `SecretIds`. It is now a `List<string>`, so use `.Add()` rather than assignment.

```csharp
// Before
options.AcceptedSecretArns = new List<string> { "arn:..." };

// After
options.SecretIds.Add("arn:...");
```

## `PollingInterval` → `ReloadInterval`

```csharp
// Before
options.PollingInterval = TimeSpan.FromMinutes(5);

// After
options.ReloadInterval = TimeSpan.FromMinutes(5);
```

## `AddSecretsManager` overloads changed

The old overload accepting `AWSCredentials?` and `RegionEndpoint?` has been removed:

```csharp
// Before (removed)
builder.AddSecretsManager(credentials, region, configurator);
```

New overloads:

```csharp
// No-arg / configure-only
builder.AddSecretsManager(options => { ... });

// With AWSOptions
builder.AddSecretsManager(awsOptions, options => { ... });

// With a pre-built IAmazonSecretsManager client
builder.AddSecretsManager(client, options => { ... });
```

## Public namespace for exception and context types

`MissingSecretValueException` and `SecretValueContext` have been moved from the `Kralizek.Extensions.Configuration.Internal` namespace to `Kralizek.Extensions.Configuration`.

```csharp
// Before
using Kralizek.Extensions.Configuration.Internal;

// After
using Kralizek.Extensions.Configuration;
```

## Newtonsoft.Json removed

The package no longer depends on Newtonsoft.Json. JSON parsing is now done with `System.Text.Json`.

## `DuplicateKeyHandling` enum added

A new `DuplicateKeyHandling` enum controls what happens when two secrets produce the same configuration key. The default is `LastWins` (matching v1.x behaviour).

```csharp
options.DuplicateKeyHandling = DuplicateKeyHandling.LastWins;  // default
options.DuplicateKeyHandling = DuplicateKeyHandling.FirstWins;
options.DuplicateKeyHandling = DuplicateKeyHandling.Throw;
```

## New logging infrastructure

A structured logging pipeline has been added:

- `SecretsManagerLogEvent` — represents a single log entry
- `SecretsManagerLogEvents` — well-known `EventId` constants
- `SecretsManagerLogging.ToLogEventSink(ILogger)` — converts to a standard `ILogger` sink
- `options.UseLogging(ILogger)` — convenience extension
- `options.UseBootstrapLogging(ILoggerFactory)` — bootstrap logging before DI is ready

## `ListSecretsFilters` is a `List<Filter>`

`ListSecretsFilters` is now a read-only `List<Filter>` property. Use `.Add()` rather than assigning a new list.

```csharp
// Before
options.ListSecretsFilters = new List<Filter> { ... };

// After
options.ListSecretsFilters.Add(new Filter { ... });
```
