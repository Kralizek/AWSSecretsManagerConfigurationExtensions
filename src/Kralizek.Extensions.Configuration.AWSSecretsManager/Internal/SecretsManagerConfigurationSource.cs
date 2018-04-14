using System;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationSource : IConfigurationSource
    {
        public SecretsManagerConfigurationSource(AWSCredentials credentials)
        {
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        public AWSCredentials Credentials {get; }

        public RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SecretsManagerConfigurationProvider(Credentials, Region);
        }
    }

}