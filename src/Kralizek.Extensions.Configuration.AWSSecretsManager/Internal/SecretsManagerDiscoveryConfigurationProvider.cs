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
    /// Configuration provider that discovers secrets via <c>ListSecrets</c> and fetches their values.
    /// </summary>
    public sealed class SecretsManagerDiscoveryConfigurationProvider : SecretsManagerConfigurationProviderBase
    {
        private readonly IAmazonSecretsManager _client;
        private readonly SecretsManagerDiscoveryOptions _options;

        /// <inheritdoc cref="SecretsManagerDiscoveryConfigurationProvider"/>
        public SecretsManagerDiscoveryConfigurationProvider(IAmazonSecretsManager client, SecretsManagerDiscoveryOptions options)
        {
            _client  = client  ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
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

        private async Task<IReadOnlyList<SecretListEntry>> FetchAllSecretsAsync(CancellationToken cancellationToken)
        {
            var result = new List<SecretListEntry>();
            ListSecretsResponse? response = null;
            do
            {
                var request = new ListSecretsRequest
                {
                    NextToken = response?.NextToken,
                    Filters = _options.ListSecretsFilters
                };
                response = await _client.ListSecretsAsync(request, cancellationToken).ConfigureAwait(false);
                result.AddRange(response.SecretList);
            } while (response.NextToken != null);
            return result;
        }

        private async Task<Dictionary<string, string?>> FetchConfigurationAsync(CancellationToken cancellationToken)
        {
            var secrets = await FetchAllSecretsAsync(cancellationToken).ConfigureAwait(false);
            var dict = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var secret in secrets)
            {
                try
                {
                    if (!_options.SecretFilter(secret))
                    {
                        Log(LogLevel.Debug, SecretsManagerLogEvents.SecretSkipped,
                            "Secret {SecretName} skipped by filter.", args: secret.Name);
                        continue;
                    }

                    var request = new GetSecretValueRequest { SecretId = secret.ARN };
                    _options.ConfigureSecretValueRequest?.Invoke(request, new SecretValueContext(secret));

                    GetSecretValueResponse secretValue;
                    try
                    {
                        secretValue = await _client.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);
                    }
                    catch (ResourceNotFoundException e)
                    {
                        throw new MissingSecretValueException(
                            $"Error retrieving secret value (Secret: {secret.Name} Arn: {secret.ARN})",
                            secret.Name, secret.ARN, e);
                    }

                    var secretString = secretValue.SecretString;
                    if (secretString is null) continue;

                    ProcessSecretString(dict, secret, secretString);
                    Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded.", args: secret.Name);
                }
                catch (MissingSecretValueException)
                {
                    throw;
                }
                catch (ResourceNotFoundException e)
                {
                    throw new MissingSecretValueException(
                        $"Error retrieving secret value (Secret: {secret.Name} Arn: {secret.ARN})",
                        secret.Name, secret.ARN, e);
                }
            }

            return dict;
        }

        private async Task<Dictionary<string, string?>> FetchConfigurationBatchAsync(CancellationToken cancellationToken)
        {
            var secrets = await FetchAllSecretsAsync(cancellationToken).ConfigureAwait(false);
            var dict = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);

            var filtered = new List<SecretListEntry>();
            foreach (var secret in secrets)
            {
                if (!_options.SecretFilter(secret))
                {
                    Log(LogLevel.Debug, SecretsManagerLogEvents.SecretSkipped,
                        "Secret {SecretName} skipped by filter.", args: secret.Name);
                    continue;
                }
                filtered.Add(secret);
            }

            var chunked = SecretsManagerHelpers.ChunkList(filtered, 20);

            foreach (var secretSet in chunked)
            {
                var request = new BatchGetSecretValueRequest
                {
                    SecretIdList = secretSet.Select(a => a.ARN).ToList()
                };
                var contextList = secretSet.Select(a => new SecretValueContext(a)).ToList();
                _options.ConfigureBatchSecretValueRequest?.Invoke(request, (IReadOnlyList<SecretValueContext>)contextList);

                try
                {
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

                        foreach (var (secretValue, secret) in secretValueSet.SecretValues
                            .Join(secretSet, sv => sv.ARN, s => s.ARN, (sv, s) => (sv, s)))
                        {
                            var secretString = secretValue.SecretString;
                            if (secretString is null) continue;

                            ProcessSecretString(dict, secret, secretString);
                            Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded (batch).", args: secret.Name);
                        }
                    } while (!string.IsNullOrWhiteSpace(secretValueSet.NextToken));
                }
                catch (ResourceNotFoundException e)
                {
                    var names = string.Join(",", secretSet.Select(a => a.Name));
                    var arns  = string.Join(",", secretSet.Select(a => a.ARN));
                    throw new MissingSecretValueException(
                        $"Error retrieving secret value (Secrets: {names} Arns: {arns})", names, arns, e);
                }
            }

            return dict;
        }

        private void ProcessSecretString(Dictionary<string, string?> dict, SecretListEntry secret, string secretString)
        {
            if (SecretsManagerHelpers.TryParseJson(secretString, out var jElement))
            {
                foreach (var (key, value) in SecretsManagerHelpers.ExtractValues(jElement!, secret.Name))
                {
                    var configKey = _options.KeyGenerator(secret, key);
                    ApplyEntry(dict, configKey, value);
                }
            }
            else
            {
                var configKey = _options.KeyGenerator(secret, secret.Name);
                ApplyEntry(dict, configKey, secretString);
            }
        }
    }
}
