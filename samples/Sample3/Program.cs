using System;
using Microsoft.Extensions.Configuration;

namespace Sample3
{
    class Program
    {
        static void Main(string[] args)
        {
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
                builder.AddSecretsManager(credentials, profile.Region);
            }
            
            var configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
