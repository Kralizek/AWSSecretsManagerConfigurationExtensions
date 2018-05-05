using System;
using Microsoft.Extensions.Configuration;

namespace Sample1
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            /*
                Uses default credentials
                Uses default region (us-east-1)
            */
            builder.AddSecretsManager();

            var configuration = builder.Build();

            Console.WriteLine("Hello World!");
        }
    }
}
