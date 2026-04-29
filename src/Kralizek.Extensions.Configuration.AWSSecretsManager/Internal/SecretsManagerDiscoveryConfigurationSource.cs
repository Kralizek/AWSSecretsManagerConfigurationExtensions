using System;

using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;

using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal sealed class SecretsManagerDiscoveryConfigurationSource : IConfigurationSource
    {
        private readonly IAmazonSecretsManager? _client;
        private readonly AWSOptions? _awsOptions;
        private readonly SecretsManagerDiscoveryOptions _options;

        public SecretsManagerDiscoveryConfigurationSource(SecretsManagerDiscoveryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerDiscoveryConfigurationSource(AWSOptions awsOptions, SecretsManagerDiscoveryOptions options)
        {
            _awsOptions = awsOptions ?? throw new ArgumentNullException(nameof(awsOptions));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerDiscoveryConfigurationSource(IAmazonSecretsManager client, SecretsManagerDiscoveryOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        internal SecretsManagerDiscoveryOptions Options => _options;
        internal IAmazonSecretsManager? Client => _client;
        internal AWSOptions? AwsOptions => _awsOptions;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = _client ?? CreateClient();
            return new SecretsManagerDiscoveryConfigurationProvider(client, _options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (_awsOptions != null)
                return _awsOptions.CreateServiceClient<IAmazonSecretsManager>();
            return new AmazonSecretsManagerClient();
        }
    }
}