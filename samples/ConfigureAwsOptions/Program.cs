// Use case: I need to configure region, profile, or other AWS SDK options.
//
// Pass an AWSOptions instance to any AddSecretsManager* method to control
// which region and credentials profile the client uses.

using Amazon.Extensions.NETCore.Setup;

using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

var awsOptions = new AWSOptions { Region = Amazon.RegionEndpoint.EUWest1 };
builder.AddSecretsManagerDiscovery(awsOptions);

var configuration = builder.Build();