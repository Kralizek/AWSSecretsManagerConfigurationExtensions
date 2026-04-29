using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// Configuration provider that fetches a configured set of known secrets.
    /// Never calls <c>ListSecrets</c>. Uses <c>BatchGetSecretValue</c> by default.
    /// </summary>
    public sealed class SecretsManagerKnownSecretsConfigurationProvider : SecretsManagerConfigurationProviderBase
    {
        private readonly IAmazonSecretsManager _client;
        private readonly IReadOnlyList<string> _secretIds;
        private readonly SecretsManagerKnownSecretsOptions _options;

        /// <inheritdoc cref="SecretsManagerKnownSecretsConfigurationProvider"/>
        public SecretsManagerKnownSecretsConfigurationProvider(IAmazonSecretsManager client, IReadOnlyList<string> secretIds, SecretsManagerKnownSecretsOptions options)
        {
            _client    = client    ?? throw new ArgumentNullException(nameof(client));
            _secretIds = secretIds ?? throw new ArgumentNullException(nameof(secretIds));
            _options   = options   ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        protected override TimeSpan? ReloadInterval => _options.ReloadInterval;

        /// <inheritdoc/>
        protected override Action<SecretsManagerLogEvent>? LogEventSink => _options.LogEvent;

        /// <inheritdoc/>
        protected override DuplicateKeyHandling DuplicateKeyHandling => _options.DuplicateKeyHandling;

        /// <inheritdoc/>
        protected override Task<Dictionary<string, string?>> FetchConfigurationCoreAsync(CancellationToken cancellationToken)
            => _options.UseBatchFetch
                ? FetchConfigurationBatchAsync(cancellationToken)
                : FetchConfigurationAsync(cancellationToken);

        private async Task<Dictionary<string, string?>> FetchConfigurationAsync(CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var secretId in _secretIds)
            {
                var syntheticEntry = new SecretListEntry { ARN = secretId, Name = secretId };
                var request = new GetSecretValueRequest { SecretId = secretId };
                _options.ConfigureSecretValueRequest?.Invoke(request, new SecretValueContext(syntheticEntry));

                GetSecretValueResponse secretValue;
                try
                {
                    secretValue = await _client.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (ResourceNotFoundException e)
                {
                    throw new MissingSecretValueException(
                        $"Error retrieving secret value (SecretId: {secretId})",
                        secretId, secretId, e);
                }

                var rootKey = !string.IsNullOrEmpty(secretValue.Name) ? secretValue.Name : secretId;
                var secretEntry = new SecretListEntry { ARN = secretValue.ARN, Name = rootKey };

                var secretString = secretValue.SecretString;
                if (secretString is null) continue;

                ProcessSecretString(dict, secretEntry, rootKey, secretString);
                Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded.", args: rootKey);
            }

            return dict;
        }

        private async Task<Dictionary<string, string?>> FetchConfigurationBatchAsync(CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);
            var chunked = SecretsManagerHelpers.ChunkList(_secretIds, 20);

            foreach (var secretIdSet in chunked)
            {
                var syntheticEntries = secretIdSet.Select(id => new SecretListEntry { ARN = id, Name = id }).ToList();

                var request = new BatchGetSecretValueRequest
                {
                    SecretIdList = secretIdSet.ToList()
                };
                var contextList = syntheticEntries.Select(e => new SecretValueContext(e)).ToList();
                _options.ConfigureBatchSecretValueRequest?.Invoke(request, (IReadOnlyList<SecretValueContext>)contextList);

                try
                {
                    var responseMap = new Dictionary<string, SecretValueEntry>(StringComparer.OrdinalIgnoreCase);

                    BatchGetSecretValueResponse? secretValueSet = null;
                    do
                    {
                        request.NextToken = secretValueSet?.NextToken;
                        secretValueSet = await _client.BatchGetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);

                        if (secretValueSet.Errors?.Any() == true)
                        {
                            var errors = SecretsManagerHelpers.HandleBatchErrors(secretValueSet);
                            throw new AggregateException(errors);
                        }

                        foreach (var sv in secretValueSet.SecretValues)
                        {
                            if (!string.IsNullOrEmpty(sv.ARN))
                                responseMap[sv.ARN] = sv;
                            if (!string.IsNullOrEmpty(sv.Name))
                                responseMap[sv.Name] = sv;
                        }
                    } while (!string.IsNullOrWhiteSpace(secretValueSet.NextToken));

                    // Process in the order of configured secretIds to respect consumer-supplied ordering.
                    // A configured id may be a short name, a full ARN, or a partial ARN, so we fall back
                    // to a prefix match against the full ARN when an exact lookup misses.
                    foreach (var secretId in secretIdSet)
                    {
                        if (!responseMap.TryGetValue(secretId, out var secretValue))
                        {
                            // Partial-ARN fallback: the caller may have supplied a prefix of the full ARN
                            secretValue = responseMap.Values
                                .FirstOrDefault(sv => !string.IsNullOrEmpty(sv.ARN)
                                    && sv.ARN.StartsWith(secretId, StringComparison.OrdinalIgnoreCase));
                        }

                        if (secretValue is null)
                        {
                            throw new MissingSecretValueException(
                                $"Error retrieving secret value (SecretId: {secretId})", secretId, secretId,
                                new ResourceNotFoundException($"Secret '{secretId}' was not found in the batch response."));
                        }

                        var rootKey = !string.IsNullOrEmpty(secretValue.Name) ? secretValue.Name : secretId;
                        var secretEntry = new SecretListEntry { ARN = secretValue.ARN, Name = rootKey };

                        var secretString = secretValue.SecretString;
                        if (secretString is null) continue;

                        ProcessSecretString(dict, secretEntry, rootKey, secretString);
                        Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded (batch).", args: rootKey);
                    }
                }
                catch (AggregateException) { throw; }
                catch (MissingSecretValueException) { throw; }
                catch (ResourceNotFoundException e)
                {
                    var configuredIds = string.Join(",", secretIdSet);
                    throw new MissingSecretValueException(
                        $"Error retrieving secret value (Configured secret ids: {configuredIds})", configuredIds, configuredIds, e);
                }
            }

            return dict;
        }

        private void ProcessSecretString(Dictionary<string, string?> dict, SecretListEntry secretEntry, string rootKey, string secretString)
        {
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
        }
    }
}
