using Amazon;
using Amazon.Runtime;
using Kralizek.Extensions.Configuration.Internal;

namespace Microsoft.Extensions.Configuration
{
    public static class SecretsManagerExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, AWSCredentials credentials, RegionEndpoint region)
        {
            configurationBuilder.Add(new SecretsManagerConfigurationSource(credentials) { Region = region });

            return configurationBuilder;
        }

        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, RegionEndpoint region)
        {
            configurationBuilder.Add(new SecretsManagerConfigurationSource() { Region = region });

            return configurationBuilder;
        }
    }
}