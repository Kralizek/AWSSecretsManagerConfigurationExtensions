using System;
using Microsoft.Extensions.Configuration;

namespace Sample5
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            /*
                Uses default credentials
                Uses default region (us-east-1)
                Uses options to customize how keys are generated (all uppercase)
            */
            builder.AddSecretsManager(configurator: options =>
            {
                options.KeyGenerator = (entry, key) => key.ToUpper();
            });

            var configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
