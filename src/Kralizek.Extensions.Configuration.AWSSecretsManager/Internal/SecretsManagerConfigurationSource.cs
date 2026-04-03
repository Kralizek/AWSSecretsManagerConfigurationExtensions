using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal class SecretsManagerConfigurationSource : IConfigurationSource
    {
        private readonly IAmazonSecretsManager? _client;
        private readonly AWSOptions? _awsOptions;
        private readonly SecretsManagerOptions _options;

        public SecretsManagerConfigurationSource(SecretsManagerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerConfigurationSource(AWSOptions awsOptions, SecretsManagerOptions options)
        {
            _awsOptions = awsOptions ?? throw new ArgumentNullException(nameof(awsOptions));
            _options    = options    ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerConfigurationSource(IAmazonSecretsManager client, SecretsManagerOptions options)
        {
            _client  = client  ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = _client ?? CreateClient();
            return new SecretsManagerConfigurationProvider(client, _options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (_awsOptions != null)
                return _awsOptions.CreateServiceClient<IAmazonSecretsManager>();

            return new AmazonSecretsManagerClient();
        }
    }
}
