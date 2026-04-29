using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

// Load exactly one secret by name or ARN.
// Required IAM permission: secretsmanager:GetSecretValue only —
// no secretsmanager:ListSecrets or secretsmanager:BatchGetSecretValue needed.
builder.AddSecretsManagerKnownSecret("my-app/prod");

var configuration = builder.Build();
