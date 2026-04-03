using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManager(options =>
{
    options.SecretIds.Add("MySecretFullARN-abcxyz");
    options.SecretIds.Add("MySecretPartialARN");
    options.SecretIds.Add("MySecretUniqueName");
});

var configuration = builder.Build();
