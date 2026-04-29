using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerDiscovery();

var configuration = builder.Build();
