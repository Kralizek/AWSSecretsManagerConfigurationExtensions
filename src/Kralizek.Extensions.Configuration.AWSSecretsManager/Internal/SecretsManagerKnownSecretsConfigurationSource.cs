using System;
using System.Collections.Generic;

using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;

using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal sealed class SecretsManagerKnownSecretsConfigurationSource : IConfigurationSource
    {
        private readonly IAmazonSecretsManager? _client;
        private readonly AWSOptions? _awsOptions;
        private readonly IReadOnlyList<string> _secretIds;
        private readonly SecretsManagerKnownSecretsOptions _options;

        public SecretsManagerKnownSecretsConfigurationSource(IReadOnlyList<string> secretIds, SecretsManagerKnownSecretsOptions options)
        {
            _secretIds = secretIds ?? throw new ArgumentNullException(nameof(secretIds));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerKnownSecretsConfigurationSource(AWSOptions awsOptions, IReadOnlyList<string> secretIds, SecretsManagerKnownSecretsOptions options)
        {
            _awsOptions = awsOptions ?? throw new ArgumentNullException(nameof(awsOptions));
            _secretIds = secretIds ?? throw new ArgumentNullException(nameof(secretIds));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerKnownSecretsConfigurationSource(IAmazonSecretsManager client, IReadOnlyList<string> secretIds, SecretsManagerKnownSecretsOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _secretIds = secretIds ?? throw new ArgumentNullException(nameof(secretIds));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        internal SecretsManagerKnownSecretsOptions Options => _options;
        internal IAmazonSecretsManager? Client => _client;
        internal AWSOptions? AwsOptions => _awsOptions;
        internal IReadOnlyList<string> SecretIds => _secretIds;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = _client ?? CreateClient();
            return new SecretsManagerKnownSecretsConfigurationProvider(client, _secretIds, _options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (_awsOptions != null)
                return _awsOptions.CreateServiceClient<IAmazonSecretsManager>();
            return new AmazonSecretsManagerClient();
        }
    }
}