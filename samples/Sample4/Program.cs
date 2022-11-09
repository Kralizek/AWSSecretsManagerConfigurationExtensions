using System;
using Kralizek.Extensions.Configuration;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

/*
    Uses default credentials
    Uses default region
    Accepts only a fixed set of secrets, by their ARN
*/

var acceptedARNs = new[]
{
    "MySecretFullARN-abcxyz",
    "MySecretPartialARN",
    "MySecretUniqueName"
};

var secretsManagerConfiguration = new SecretsManagerConfiguration();
secretsManagerConfiguration.AcceptedSecretArns.AddRange(acceptedARNs);

builder.AddSecretsManager(secretsManagerConfiguration);

var configuration = builder.Build();

Console.WriteLine("Hello World!");