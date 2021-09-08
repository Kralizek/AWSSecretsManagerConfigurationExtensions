using System;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
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

            if (options.ReadFromConfig)
                ReadFromConfig(configurationBuilder, ref options, ref region, ref credentials);

            var source = new SecretsManagerConfigurationSource(credentials, options, region);

            configurationBuilder.Add(source);

            return configurationBuilder;
        }

        private static void ReadFromConfig(
            IConfigurationBuilder configurationBuilder,
            ref SecretsManagerConfigurationProviderOptions options,
            ref RegionEndpoint? region, 
            ref AWSCredentials? credentials)
        {
            if (!options.ReadFromConfig) return;
            
            var config = configurationBuilder.Build();
                
            var awsOptions = config.GetAWSOptions();

            if (awsOptions == null) return;
            
            var smConfig = config.GetSection(options.ConfigSectionName).Get<SecretsManagerConfigurationSection>();
            
            if (smConfig.ListSecretsFilters != null) options.ListSecretsFilters?.AddRange(smConfig.ListSecretsFilters);
            if (smConfig.AcceptedSecretArns != null) options.AcceptedSecretArns?.AddRange(smConfig.AcceptedSecretArns);
            
            options.PollingInterval ??= smConfig.PollingIntervalInSeconds.HasValue
                ? TimeSpan.FromSeconds(smConfig.PollingIntervalInSeconds.Value)
                : null;

            credentials ??= GetCredentials(awsOptions);
                    
            region ??= smConfig?.Region != null
                ? RegionEndpoint.GetBySystemName(smConfig.Region)
                : awsOptions?.Region;
        }

        private static AWSCredentials GetCredentials(AWSOptions awsOptions)
        {
            if (awsOptions.Profile == null)
                return FallbackCredentialsFactory.GetCredentials();

            var chain = new CredentialProfileStoreChain(awsOptions.ProfilesLocation);

            chain.TryGetAWSCredentials(awsOptions.Profile, out var credentials);
            
            return credentials ?? FallbackCredentialsFactory.GetCredentials();
        }
    }
}