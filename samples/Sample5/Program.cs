using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerDiscovery(options =>
{
    options.KeyGenerator = context => context.DefaultKey.ToUpper();
});

var configuration = builder.Build();