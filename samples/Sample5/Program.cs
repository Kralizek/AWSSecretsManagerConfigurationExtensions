using System;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

/*
    Uses default credentials
    Uses default region
    Uses options to customize how keys are generated (all uppercase)
*/
builder.AddSecretsManager(options =>
{
    options.Configure(o => o.KeyGenerator = (entry, key) => key.ToUpper());
});

var configuration = builder.Build();

Console.WriteLine("Hello World!");