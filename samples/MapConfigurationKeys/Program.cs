// Use case: I want secret names and JSON properties to bind nicely into IConfiguration.
//
// SecretKeyMappingOptions controls how secret names and JSON property paths are projected
// into .NET configuration keys. Use KeyMapping before reaching for KeyGenerator.
//
// SecretNamePathSeparator = "/" is the new default. It maps path-style secret names such as:
//   /my-app/production/database  →  my-app:production:database
// Set SecretNamePathSeparator = null to restore verbatim secret-name behavior (1.x compatible).

using Microsoft.Extensions.Configuration;

// ── Example 1: Load a JSON secret directly as normal application configuration ─────────────────
// With PrefixJsonKeysWithSecretName = false, the secret name is stripped and only the
// JSON property path is used. A JSON secret {"Smtp":{"Host":"mail.example.com","Port":587}}
// stored as "/my-app/email" produces:
//   Smtp:Host = mail.example.com
//   Smtp:Port = 587

var builder1 = new ConfigurationBuilder();
builder1.AddSecretsManagerKnownSecret("/my-app/email", options =>
{
    options.KeyMapping.PrefixJsonKeysWithSecretName = false;
});

// ── Example 2: Load a JSON secret into a target section ───────────────────────────────────────
// TargetSection prepends a section name to all generated keys. Combined with
// PrefixJsonKeysWithSecretName = false, this projects the JSON directly under "Email":
//   Email:Smtp:Host = mail.example.com
//   Email:Smtp:Port = 587

var builder2 = new ConfigurationBuilder();
builder2.AddSecretsManagerKnownSecret("/my-app/email", options =>
{
    options.KeyMapping.PrefixJsonKeysWithSecretName = false;
    options.KeyMapping.TargetSection = "Email";
});

// ── Example 3: Restore verbatim secret-name behavior (1.x compatibility) ─────────────────────
// Set SecretNamePathSeparator = null to disable path normalization. The secret name
// "/my-app/production/database" is used as-is without converting "/" to ":".

var builder3 = new ConfigurationBuilder();
builder3.AddSecretsManagerKnownSecret("/my-app/production/database", options =>
{
    options.KeyMapping.SecretNamePathSeparator = null;
});

builder1.Build();
builder2.Build();
builder3.Build();