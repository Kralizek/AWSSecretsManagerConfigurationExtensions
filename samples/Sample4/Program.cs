using System;
using System.Linq;
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

builder.AddSecretsManager(configurator: options =>
{
    options.AcceptedSecretArns.AddRange(acceptedARNs);
});

var configuration = builder.Build();

Console.WriteLine("Hello World!");