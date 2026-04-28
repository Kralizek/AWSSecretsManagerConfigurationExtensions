using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

var awsOptions = new AWSOptions { Region = Amazon.RegionEndpoint.EUWest1 };
builder.AddSecretsManager(awsOptions);

var configuration = builder.Build();
