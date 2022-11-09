using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    public sealed class SecretsManagerConfigurationProviderOptions
    {
        public SecretsManagerConfigurationProviderOptions(AWSOptions awsOptions, SecretsManagerConfiguration secretsManagerConfiguration)
        {
            AWSOptions = awsOptions ?? throw new ArgumentNullException(nameof(awsOptions));
            SecretsManagerConfiguration = secretsManagerConfiguration ?? throw new ArgumentNullException(nameof(secretsManagerConfiguration));
        }

        public SecretsManagerConfigurationProviderOptions() : this(new AWSOptions(), new SecretsManagerConfiguration()) { }
        
        public AWSOptions AWSOptions { get; }

        public SecretsManagerConfiguration SecretsManagerConfiguration { get; }

        /// <summary>
        /// A function that can be used to configure the <see cref="AmazonSecretsManagerClient"/>
        /// that's injected into the client.
        /// </summary>
        /// <example>
        /// <code>
        /// ConfigureClient = config => config.Timeout = TimeSpan.FromSeconds(5);
        /// </code>
        /// </example>
        public Action<ClientConfig> ConfigureClient { get; set; } = _ => { };
    }
}