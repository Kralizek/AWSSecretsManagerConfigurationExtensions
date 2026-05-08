// Use case: I want to combine multiple configuration sources and rely on normal
// .NET configuration precedence.
//
// In .NET configuration, sources added later override sources added earlier.
// This sample layers a local JSON file with two Secrets Manager calls so that
// production secrets override shared defaults, which in turn override local settings.
//
// Source order (lowest to highest precedence):
//   1. appsettings.json        — local defaults (checked into source control)
//   2. /my-app/shared          — shared secrets common to all environments
//   3. /my-app/production      — production-specific secrets (override shared ones)
//
// Note: this is different from ResolveDuplicateKeys, which handles duplicate keys
// produced inside a single Secrets Manager provider call.
// Use ComposeConfigurationSources when you want normal .NET provider-level precedence.

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddJsonFile("appsettings.json", optional: true);

builder.AddSecretsManagerKnownSecret(
    "/my-app/shared",
    options =>
    {
        options.KeyMapping.PrefixJsonKeysWithSecretName = false;
    });

builder.AddSecretsManagerKnownSecret(
    "/my-app/production",
    options =>
    {
        options.KeyMapping.PrefixJsonKeysWithSecretName = false;
    });

builder.Build();