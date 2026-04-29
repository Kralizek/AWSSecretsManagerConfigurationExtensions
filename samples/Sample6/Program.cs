using Kralizek.Extensions.Configuration;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerDiscovery(options =>
{
    options.DuplicateKeyHandling = DuplicateKeyHandling.LastWins;
});

var configuration = builder.Build();
