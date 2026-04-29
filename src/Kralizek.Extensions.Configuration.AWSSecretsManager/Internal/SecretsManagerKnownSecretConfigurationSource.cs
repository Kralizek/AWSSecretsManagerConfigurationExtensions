using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal sealed class SecretsManagerKnownSecretConfigurationSource : IConfigurationSource
    {
        private readonly IAmazonSecretsManager? _client;
        private readonly AWSOptions? _awsOptions;
        private readonly string _secretId;
        private readonly SecretsManagerKnownSecretOptions _options;

        public SecretsManagerKnownSecretConfigurationSource(string secretId, SecretsManagerKnownSecretOptions options)
        {
            _secretId = secretId ?? throw new ArgumentNullException(nameof(secretId));
            _options  = options  ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerKnownSecretConfigurationSource(AWSOptions awsOptions, string secretId, SecretsManagerKnownSecretOptions options)
        {
            _awsOptions = awsOptions ?? throw new ArgumentNullException(nameof(awsOptions));
            _secretId   = secretId   ?? throw new ArgumentNullException(nameof(secretId));
            _options    = options    ?? throw new ArgumentNullException(nameof(options));
        }

        public SecretsManagerKnownSecretConfigurationSource(IAmazonSecretsManager client, string secretId, SecretsManagerKnownSecretOptions options)
        {
            _client   = client   ?? throw new ArgumentNullException(nameof(client));
            _secretId = secretId ?? throw new ArgumentNullException(nameof(secretId));
            _options  = options  ?? throw new ArgumentNullException(nameof(options));
        }

        internal SecretsManagerKnownSecretOptions Options => _options;
        internal IAmazonSecretsManager? Client => _client;
        internal AWSOptions? AwsOptions => _awsOptions;
        internal string SecretId => _secretId;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var client = _client ?? CreateClient();
            return new SecretsManagerKnownSecretConfigurationProvider(client, _secretId, _options);
        }

        private IAmazonSecretsManager CreateClient()
        {
            if (_awsOptions != null)
                return _awsOptions.CreateServiceClient<IAmazonSecretsManager>();
            return new AmazonSecretsManagerClient();
        }
    }
}
