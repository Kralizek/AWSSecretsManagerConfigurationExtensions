using System;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder();

/*
    Uses credentials obtained from store
    Uses default region (from stored profile)
    Uses default options
*/
var chain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain();

if (chain.TryGetProfile("MyProfile", out var profile))
{
    var credentials = profile.GetAWSCredentials(profile.CredentialProfileStore);

    builder.AddSecretsManager(new AWSOptions { Region = profile.Region, Credentials = credentials });
}
            
var configuration = builder.Build();

Console.WriteLine("Hello World!");