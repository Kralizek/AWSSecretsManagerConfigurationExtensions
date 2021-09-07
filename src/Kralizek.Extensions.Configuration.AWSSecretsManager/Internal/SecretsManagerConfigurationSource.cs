using System;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationSource : IConfigurationSource
    {
        public SecretsManagerConfigurationSource(AWSCredentials? credentials = null, SecretsManagerConfigurationProviderOptions? options = null, RegionEndpoint? region = null)
        {
            Credentials = credentials;
            Options = options ?? new SecretsManagerConfigurationProviderOptions();
            Region = region;
        }

        public SecretsManagerConfigurationProviderOptions Options { get; }

        public AWSCredentials? Credentials { get; private set; }

        public RegionEndpoint? Region { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = CreateClient(builder);
            
            return new SecretsManagerConfigurationProvider(client, Options);
        }

        private IAmazonSecretsManager CreateClient(IConfigurationBuilder builder)
        {
            if (Options.CreateClient != null)
            {
                return Options.CreateClient();
            }

            if (Credentials == null || Region == null)
            {
                var awsOptions = builder.Build().GetAWSOptions();
                Region ??= awsOptions?.Region;
                Credentials ??= CreateCredentials(awsOptions);
            }

            var clientConfig = new AmazonSecretsManagerConfig
            {
                RegionEndpoint = Region
            };

            Options.ConfigureSecretsManagerConfig(clientConfig);

            return Credentials switch
            {
                null => new AmazonSecretsManagerClient(clientConfig),
                _ => new AmazonSecretsManagerClient(Credentials, clientConfig)
            };
        }
        
        private AWSCredentials CreateCredentials(AWSOptions awsOptions)
        {
            if (awsOptions.Profile == null)
                return FallbackCredentialsFactory.GetCredentials();

            var chain = new CredentialProfileStoreChain(awsOptions.ProfilesLocation);

            chain.TryGetAWSCredentials(awsOptions.Profile, out var credentials);
            
            return credentials ?? FallbackCredentialsFactory.GetCredentials();
        }
    }

}