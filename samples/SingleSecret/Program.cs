// Use case: I know exactly one secret to load.
//
// AddSecretsManagerKnownSecret is the least-privilege loading mode.
// It fetches exactly one secret by name or ARN using a single GetSecretValue call.
//
// Required IAM permissions:
//   secretsmanager:GetSecretValue
//
// This mode does NOT require:
//   secretsmanager:ListSecrets
//   secretsmanager:BatchGetSecretValue
//
// For a JSON secret such as {"Smtp":{"Host":"mail.example.com","Port":587}}, the
// configuration keys produced are (with the default SecretNamePathSeparator = "/"):
//   my-app:prod:Smtp:Host = mail.example.com
//   my-app:prod:Smtp:Port = 587

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerKnownSecret("my-app/prod");

var configuration = builder.Build();