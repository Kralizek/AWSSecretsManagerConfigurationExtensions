// Use case: I know the exact set of secrets to load.
//
// AddSecretsManagerKnownSecrets fetches a fixed list of secrets by name or ARN.
// It never calls ListSecrets, so you can omit secretsmanager:ListSecrets from your IAM policy.
//
// Required IAM permissions:
//   secretsmanager:BatchGetSecretValue  (default, when UseBatchFetch = true)
//   secretsmanager:GetSecretValue       (if UseBatchFetch = false)
//
// Configuration keys produced from "/my-app/production" with JSON value {"Host":"db.example.com"}:
//   my-app:production:Host = db.example.com    (PrefixJsonKeysWithSecretName = true, the default)

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerKnownSecrets(
    ["/my-app/shared", "/my-app/production"],
    options =>
    {
        options.UseBatchFetch = true; // default — fetches all secrets in a single API call
    });

builder.Build();