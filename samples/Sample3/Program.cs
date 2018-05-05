using System;
using Amazon.Runtime;
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
                Uses default region (us-east-1)
            */
            
            var chain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain();

            if (chain.TryGetAWSCredentials("MyProfile", out var credentials))
            {
                builder.AddSecretsManager(credentials);
            }
            
            var configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
