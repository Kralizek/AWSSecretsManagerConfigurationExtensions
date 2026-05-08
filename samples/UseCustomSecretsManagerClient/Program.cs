// Use case: I already have or need to build the AWS Secrets Manager client myself.
//
// Pass a caller-created IAmazonSecretsManager instance directly.
// The provider will use this client as-is and will not dispose it.

using Amazon;
using Amazon.SecretsManager;

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

var client = new AmazonSecretsManagerClient(RegionEndpoint.EUWest1);
builder.AddSecretsManagerDiscovery(client);

var configuration = builder.Build();