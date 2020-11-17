using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager.Model;
using Kralizek.Extensions.Configuration.Internal;
// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration
{
    public static class SecretsManagerExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder,
            AWSCredentials? credentials = null,
            RegionEndpoint? region = null,
            Action<SecretsManagerConfigurationProviderOptions>? configurator = null)
        {
            var options = new SecretsManagerConfigurationProviderOptions();

            configurator?.Invoke(options);

            var source = new SecretsManagerConfigurationSource(credentials, options);

            if (region is not null)
            {
                source.Region = region;
            }

            configurationBuilder.Add(source);

            return configurationBuilder;
        }
    }
}