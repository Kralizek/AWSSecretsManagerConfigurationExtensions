using Amazon;
using Amazon.Runtime;
using Kralizek.Extensions.Configuration.Internal;
// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration
{
    public static class SecretsManagerExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, AWSCredentials credentials = null, RegionEndpoint region = null)
        {
            var source = new SecretsManagerConfigurationSource(credentials);
            
            if (region != null)
            {
                source.Region = region;
            }

            configurationBuilder.Add(source);

            return configurationBuilder;
        }
    }
}