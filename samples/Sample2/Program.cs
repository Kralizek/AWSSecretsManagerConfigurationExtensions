using System;
using Amazon;
using Microsoft.Extensions.Configuration;

namespace Sample2
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            /*
                Uses default credentials
                Uses eu-west-1 region
                Uses default options
            */
            builder.AddSecretsManager(region: RegionEndpoint.EUWest1);

            var configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
