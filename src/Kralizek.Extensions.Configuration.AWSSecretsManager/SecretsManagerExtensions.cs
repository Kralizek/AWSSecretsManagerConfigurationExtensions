using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    public static class SecretsManagerExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(
            this IConfigurationBuilder builder,
            Action<SecretsManagerOptions>? configure = null)
        {
            var options = new SecretsManagerOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerConfigurationSource(options));
            return builder;
        }

        public static IConfigurationBuilder AddSecretsManager(
            this IConfigurationBuilder builder,
            AWSOptions awsOptions,
            Action<SecretsManagerOptions>? configure = null)
        {
            var options = new SecretsManagerOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerConfigurationSource(awsOptions, options));
            return builder;
        }

        public static IConfigurationBuilder AddSecretsManager(
            this IConfigurationBuilder builder,
            IAmazonSecretsManager client,
            Action<SecretsManagerOptions>? configure = null)
        {
            var options = new SecretsManagerOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerConfigurationSource(client, options));
            return builder;
        }
    }
}
