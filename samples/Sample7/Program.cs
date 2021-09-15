using Microsoft.Extensions.Configuration;

namespace Sample7
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            /* 
             * Add some configuration source. 
             * In this case, we're adding our appsettings JSON file.
             */
            builder.AddJsonFile("appsettings.json");

            /*
             * Add the secrets manager
             */
            builder.AddSecretsManager(configurator: op =>
            {
                /*
                 * Configure the secrets manager to read options 
                 * from a certain config section
                 */
                op.ReadFromConfigSection("MySecretsManagerConfig");

                /*
                 * Can still use the other options here
                 */
                op.KeyGenerator = (entry, key) => key.ToUpper();
            });
        }
    }
}
