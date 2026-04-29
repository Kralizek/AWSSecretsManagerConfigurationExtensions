using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerDiscovery(options =>
{
    options.KeyGenerator = (entry, key) => key.ToUpper();
});

var configuration = builder.Build();
