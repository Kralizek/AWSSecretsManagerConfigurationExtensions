using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationSource : IConfigurationSource
    {
        public SecretsManagerConfigurationSource(AWSCredentials credentials = null)
        {
            Credentials = credentials;
        }

        public AWSCredentials Credentials {get; }

        public RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = CreateClient();
            return new SecretsManagerConfigurationProvider(client);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (Credentials == null)
            {
                return new AmazonSecretsManagerClient(Region);
            }

            return new AmazonSecretsManagerClient(Credentials, Region);
        }
    }

}