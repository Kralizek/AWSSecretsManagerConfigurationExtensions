using System;
using Kralizek.Extensions.Configuration.Internal;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

/*
    Uses default credentials
    Uses default region
    Uses options to customize how keys are generated (all uppercase)
*/
builder.AddSecretsManager(new SecretsManagerConfiguration
{
    KeyGenerator = (entry, key) => key.ToUpper()
});

var configuration = builder.Build();

Console.WriteLine("Hello World!");