using Amazon;
using Amazon.Runtime;
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
            var client = CreateClient(); 
            
            return new SecretsManagerConfigurationProvider(client, Options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (Options.CreateClient != null)
            {
                return Options.CreateClient();
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
    }
}