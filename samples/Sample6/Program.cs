using System;

using Amazon;
using Amazon.SecretsManager;

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

/*
    Uses the specified client factory method
*/
builder.AddSecretsManager(configurator: options =>
{
    options.CreateClient = CreateClient;
});

var configuration = builder.Build();

Console.WriteLine("Hello World!");

static IAmazonSecretsManager CreateClient()
{
    return new AmazonSecretsManagerClient(RegionEndpoint.EUWest1);
}