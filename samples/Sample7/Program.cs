﻿using System;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

/*
    Uses default credentials
    Uses default region
    Uses options to customize the GetSecretValueRequest (e.g. specify VersionStage)
*/

builder.AddSecretsManager(options =>
{
    options.Configure(o => o.ConfigureSecretValueRequest = (request, context) => request.VersionStage = "AWSCURRENT");
});

var configuration = builder.Build();

Console.WriteLine("Hello World!");