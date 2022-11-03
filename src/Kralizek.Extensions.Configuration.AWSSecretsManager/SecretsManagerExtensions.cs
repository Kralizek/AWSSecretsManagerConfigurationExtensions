using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Kralizek.Extensions.Configuration.Internal;
// ReSharper disable CheckNamespace

[assembly: InternalsVisibleTo("Tests.Extensions.Configuration.AWSSecretsManager")]
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
            var config = configurationBuilder.Build();
                
            var awsConfig = config.GetSection("AWS")?.Get<AwsConfigSection>();
            var smConfig = config.GetSection(options.ConfigSectionName)?.Get<SecretsManagerConfigurationSection>();
            
            if (smConfig?.ListSecretsFilters != null)
                options.ListSecretsFilters?.AddRange(smConfig.ListSecretsFilters.Select(f => f.ToAwsFilter()));

            if (smConfig?.AcceptedSecretArns != null)
                options.AcceptedSecretArns?.AddRange(smConfig.AcceptedSecretArns);
            
            options.PollingInterval ??= smConfig?.PollingIntervalInSeconds != null
                ? TimeSpan.FromSeconds(smConfig.PollingIntervalInSeconds.Value)
                : null;

            var profile = smConfig?.Profile ?? awsConfig?.Profile;
            var profilesLocation = smConfig?.ProfilesLocation ?? awsConfig?.ProfilesLocation;
            
            credentials ??= GetCredentials(profile, profilesLocation);
                    
            var configRegionName = smConfig?.Region ?? awsConfig?.Region;
            
            region ??= configRegionName != null
                ? RegionEndpoint.GetBySystemName(configRegionName)
                : null;
        }

        private static AWSCredentials GetCredentials(string? profile, string? profilesLocation)
        {
            if (profile == null)
                return FallbackCredentialsFactory.GetCredentials();

            var chain = new CredentialProfileStoreChain(profilesLocation);

            chain.TryGetAWSCredentials(profile, out var credentials);
            
            return credentials ?? FallbackCredentialsFactory.GetCredentials();
        }
    }
}