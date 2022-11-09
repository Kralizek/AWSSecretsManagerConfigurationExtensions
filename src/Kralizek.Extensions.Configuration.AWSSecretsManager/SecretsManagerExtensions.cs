using Amazon.Extensions.NETCore.Setup;
using Kralizek.Extensions.Configuration.Internal;
// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration
{
    public static class SecretsManagerExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, SecretsManagerConfiguration configuration, AWSOptions options)
        {
            var providerOptions = new SecretsManagerConfigurationProviderOptions(options, configuration);
            
            var source = new SecretsManagerConfigurationSource(providerOptions);
            
            configurationBuilder.Add(source);

            return configurationBuilder;
        }

        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, AWSOptions options)
        {
            return AddSecretsManager(configurationBuilder, new SecretsManagerConfiguration(), options);
        }

        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, SecretsManagerConfiguration configuration)
        {
            return AddSecretsManager(configurationBuilder, configuration, new AWSOptions());
        }

        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder)
        {
            return AddSecretsManager(configurationBuilder, new SecretsManagerConfiguration(), new AWSOptions());
        }

        public static SecretsManagerConfiguration GetSecretsManagerOptions(this IConfigurationSection section)
        {
            // TODO: Implement parsing
            return new SecretsManagerConfiguration();
        }
    }
}