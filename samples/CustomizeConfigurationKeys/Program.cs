// Use case: Built-in key mapping is not enough; I need full control over configuration key names.
//
// The KeyGenerator delegate runs after built-in key mapping and receives the mapped key
// as context.DefaultKey. Use this as an advanced escape hatch when SecretKeyMappingOptions
// are not sufficient.
//
// Note: replacing "/" with ":" is no longer necessary here — SecretNamePathSeparator = "/"
// is the default and already handles that mapping. Use KeyGenerator only for logic
// that cannot be expressed with the built-in options.
//
// This example routes scalar secrets under a "ScalarSecrets" section while leaving
// JSON-derived keys unchanged.

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerDiscovery(options =>
{
    options.KeyGenerator = context =>
    {
        if (!context.HasJsonPath)
        {
            // Place scalar secrets under a dedicated section.
            return Microsoft.Extensions.Configuration.ConfigurationPath.Combine(
                "ScalarSecrets", context.SecretName);
        }

        // For JSON secrets, use the default mapped key unchanged.
        return context.DefaultKey;
    };
});

var configuration = builder.Build();