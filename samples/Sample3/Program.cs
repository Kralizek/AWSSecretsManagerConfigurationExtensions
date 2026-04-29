using Amazon;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

var client = new AmazonSecretsManagerClient(RegionEndpoint.EUWest1);
builder.AddSecretsManagerDiscovery(client);

var configuration = builder.Build();
