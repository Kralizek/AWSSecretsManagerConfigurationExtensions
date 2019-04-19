using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Sample4
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            /*
                Uses default credentials
                Uses default region
                Accepts only a fixed set of secrets, by their ARN
            */

            var acceptedARNs = new[]
            {
                "MySecretARN1",
                "MySecretARN2",
                "MySecretARN3",
            };

            builder.AddSecretsManager(configurator: options =>
            {
                options.SecretFilter = entry => acceptedARNs.Contains(entry.ARN);
            });

            var configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
