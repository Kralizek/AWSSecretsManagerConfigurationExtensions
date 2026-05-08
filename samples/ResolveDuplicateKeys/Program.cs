// Use case: Multiple secrets or multiple JSON values produce the same configuration key.
//
// This sample demonstrates DuplicateKeyHandling within a single provider call.
// When two secrets (or two JSON properties from different secrets) map to the same
// configuration key, DuplicateKeyHandling.LastWins keeps the last value seen.
//
// Note: if you want to layer multiple configuration sources with normal .NET precedence,
// see the ComposeConfigurationSources sample instead.

using Kralizek.Extensions.Configuration;

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerKnownSecrets(
    ["/my-app/shared", "/my-app/production"],
    options =>
    {
        // Strip the secret name prefix so JSON keys from both secrets land in the same namespace.
        options.KeyMapping.PrefixJsonKeysWithSecretName = false;

        // When both secrets contain a key with the same name, keep the last-loaded value.
        options.DuplicateKeyHandling = DuplicateKeyHandling.LastWins;
    });

builder.Build();