// See https://aka.ms/new-console-template for more information

using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

var configurationBuilder = new ConfigurationBuilder();

configurationBuilder.AddObject(new
{
    AWS = new
    {
        Profile = "dev",
        Region = "eu-west-1"
    },
    SecretsManagerProfile = new
    {
        Profile = "configuration",
        Region = "eu-west-2",
        ProfilesLocation = "C:\\Users\\MyUser\\.aws\\"
    },
    SecretManager = new
    {
        PollingIntervalInSeconds = 60,
        AcceptedSecretArns = new[]
        {
            "arn:example:01-abcxyz",
            "arn:example:02",
            "unique-secret-name"
        },
        ListSecretsFilters = new[]
        {
            new { Key = "Name", Values = new[] { "Value1", "Value2" } }
        }
    }
});

var configuration = configurationBuilder.Build();

configurationBuilder.AddSecretsManager(options =>
{
    options.UseConfiguration(configuration.GetSection("SecretsManager").GetSecretsManagerOptions());

    options.Configure(o => o.AcceptedSecretArns.Add("arn:example:03"));

    options.UseAWSOptions(configuration.GetSection("SecretsManagerProfile").GetAWSOptions());
});

Console.WriteLine("Hello, World!");
