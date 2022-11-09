// See https://aka.ms/new-console-template for more information

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

var secretsManagerConfiguration = configuration.GetSection("SecretsManager").GetSecretsManagerOptions();
secretsManagerConfiguration.AcceptedSecretArns.Add("arn:example:03");

var awsOptions = configuration.GetSection("SecretsManagerProfile").GetAWSOptions();
awsOptions.DefaultClientConfig.Timeout = TimeSpan.FromSeconds(1);

configurationBuilder.AddSecretsManager(secretsManagerConfiguration, awsOptions);

configuration = configurationBuilder.Build();

Console.WriteLine("Hello, World!");
