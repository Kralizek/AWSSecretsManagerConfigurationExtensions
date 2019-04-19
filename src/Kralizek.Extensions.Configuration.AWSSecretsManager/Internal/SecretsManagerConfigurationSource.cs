using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationSource : IConfigurationSource
    {
        public SecretsManagerConfigurationSource(AWSCredentials credentials = null, SecretsManagerConfigurationProviderOptions options = null)
        {
            Credentials = credentials;
            Options = options ?? new SecretsManagerConfigurationProviderOptions();
        }

        public SecretsManagerConfigurationProviderOptions Options { get; }

        public AWSCredentials Credentials {get; }

        public RegionEndpoint Region { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = CreateClient();
            return new SecretsManagerConfigurationProvider(client, Options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (Credentials == null && Region == null)
            {
                return new AmazonSecretsManagerClient();
            }

            if (Credentials == null)
            {
                return new AmazonSecretsManagerClient(Region);
            }

            if (Region == null)
            {
                return new AmazonSecretsManagerClient(Credentials);
            }

            return new AmazonSecretsManagerClient(Credentials, Region);
        }
    }

}