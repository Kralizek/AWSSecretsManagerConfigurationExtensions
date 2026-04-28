using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManager(options =>
{
    options.KeyGenerator = (entry, key) => key.ToUpper();
});

var configuration = builder.Build();
