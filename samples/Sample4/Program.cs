using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerKnownSecrets(new[]
{
    "MySecretFullARN-abcxyz",
    "MySecretPartialARN",
    "MySecretUniqueName"
});

var configuration = builder.Build();