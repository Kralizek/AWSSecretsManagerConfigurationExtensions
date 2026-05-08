// Use case: I want the provider to discover and load secrets by listing them.
// 
// This sample uses AddSecretsManagerDiscovery, which calls ListSecrets followed by
// BatchGetSecretValue (or GetSecretValue if UseBatchFetch is false).
//
// Required IAM permissions:
//   secretsmanager:ListSecrets
//   secretsmanager:BatchGetSecretValue  (or secretsmanager:GetSecretValue if UseBatchFetch = false)
//
// By default, PrefixJsonKeysWithSecretName = true, which namespaces JSON-derived keys
// under the mapped secret name. This prevents key collisions when multiple secrets
// contain properties with the same name.
//
// Configuration keys produced from secret "/my-app/production/database" with JSON value
// {"Host":"db.example.com","Port":5432} will be:
//   my-app:production:database:Host   = db.example.com
//   my-app:production:database:Port   = 5432

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerDiscovery(options =>
{
    // Apply a server-side filter to narrow the set of discovered secrets.
    // Without a filter, ALL secrets in the account/region are loaded.
    options.ListSecretsFilters.Add(new Amazon.SecretsManager.Model.Filter
    {
        Key = "name",
        Values = ["/my-app/"]
    });
});

builder.Build();