using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// Configuration provider that fetches exactly one known secret using <c>GetSecretValue</c>.
    /// Never calls <c>ListSecrets</c> or <c>BatchGetSecretValue</c>.
    /// </summary>
    public sealed class SecretsManagerKnownSecretConfigurationProvider : SecretsManagerConfigurationProviderBase
    {
        private readonly IAmazonSecretsManager _client;
        private readonly string _secretId;
        private readonly SecretsManagerKnownSecretOptions _options;

        /// <inheritdoc cref="SecretsManagerKnownSecretConfigurationProvider"/>
        public SecretsManagerKnownSecretConfigurationProvider(IAmazonSecretsManager client, string secretId, SecretsManagerKnownSecretOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _secretId = !string.IsNullOrWhiteSpace(secretId) ? secretId : throw new ArgumentException("Secret id must not be null or whitespace.", nameof(secretId));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        protected override TimeSpan? ReloadInterval => _options.ReloadInterval;

        /// <inheritdoc/>
        protected override Action<SecretsManagerLogEvent>? LogEventSink => _options.LogEvent;

        /// <inheritdoc/>
        protected override DuplicateKeyHandling DuplicateKeyHandling => _options.DuplicateKeyHandling;

        /// <inheritdoc/>
        protected override string ProviderType => "KnownSecret";

        /// <inheritdoc/>
        protected override Task<Dictionary<string, string?>> FetchConfigurationCoreAsync(CancellationToken cancellationToken)
            => FetchConfigurationAsync(cancellationToken);

        private async Task<Dictionary<string, string?>> FetchConfigurationAsync(CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);

            var request = new GetSecretValueRequest { SecretId = _secretId };
            var syntheticEntry = new SecretListEntry { ARN = _secretId, Name = _secretId };
            _options.ConfigureSecretValueRequest?.Invoke(request, new SecretValueContext(syntheticEntry));

            GetSecretValueResponse secretValue;
            using var activity = SecretsManagerInstrumentation.ActivitySource.StartActivity("secretsmanager GetSecretValue");
            activity?.SetTag("aws.secretsmanager.secret.name", _secretId);
            try
            {
                secretValue = await _client.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(secretValue.ARN))
                    activity?.SetTag("aws.secretsmanager.secret.arn", secretValue.ARN);
            }
            catch (ResourceNotFoundException e)
            {
                activity?.SetStatus(ActivityStatusCode.Error, e.Message);
                throw new MissingSecretValueException(
                    $"Error retrieving secret value (SecretId: {_secretId})",
                    _secretId, _secretId, e);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }

            var rootKey = !string.IsNullOrEmpty(secretValue.Name) ? secretValue.Name : _secretId;
            var secretEntry = new SecretListEntry { ARN = secretValue.ARN, Name = rootKey };

            var secretString = secretValue.SecretString;
            if (secretString is null) return dict;

            if (SecretsManagerHelpers.TryParseJson(secretString, out var jElement))
            {
                foreach (var (key, value) in SecretsManagerHelpers.ExtractValues(jElement!, rootKey))
                {
                    var configKey = _options.KeyGenerator(secretEntry, key);
                    ApplyEntry(dict, configKey, value);
                }
            }
            else
            {
                var configKey = _options.KeyGenerator(secretEntry, rootKey);
                ApplyEntry(dict, configKey, secretString);
            }

            Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded.", args: rootKey);
            return dict;
        }
    }
}