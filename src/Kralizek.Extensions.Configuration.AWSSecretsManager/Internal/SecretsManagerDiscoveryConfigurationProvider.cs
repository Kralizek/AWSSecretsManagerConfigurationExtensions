using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// Configuration provider that discovers secrets via <c>ListSecrets</c> and fetches their values.
    /// </summary>
    public sealed class SecretsManagerDiscoveryConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IAmazonSecretsManager _client;
        private readonly SecretsManagerDiscoveryOptions _options;

        private Dictionary<string, string?> _loadedValues = new(StringComparer.InvariantCultureIgnoreCase);
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public SecretsManagerDiscoveryConfigurationProvider(IAmazonSecretsManager client, SecretsManagerDiscoveryOptions options)
        {
            _client  = client  ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        public Task ForceReloadAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

        private void Log(LogLevel level, EventId eventId, string message, Exception? ex = null, params object?[] args)
            => _options.LogEvent?.Invoke(new SecretsManagerLogEvent(level, eventId, message, ex, Args: args));

        private async Task LoadAsync()
        {
            Log(LogLevel.Debug, SecretsManagerLogEvents.LoadStarted, "Secrets Manager configuration load started.");

            _loadedValues = _options.UseBatchFetch
                ? await FetchConfigurationBatchAsync(default).ConfigureAwait(false)
                : await FetchConfigurationAsync(default).ConfigureAwait(false);

            SetData(_loadedValues, triggerReload: false);

            Log(LogLevel.Debug, SecretsManagerLogEvents.LoadCompleted,
                "Secrets Manager configuration load completed. {SecretCount} secrets loaded.",
                args: _loadedValues.Count);

            if (_options.ReloadInterval.HasValue)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _pollingTask = PollForChangesAsync(_options.ReloadInterval.Value, _cancellationTokenSource.Token);
            }
        }

        private async Task PollForChangesAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try { await Task.Delay(interval, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                try { await ReloadAsync(cancellationToken).ConfigureAwait(false); }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Log(LogLevel.Error, SecretsManagerLogEvents.ReloadFailed, "Secrets Manager configuration reload failed.", ex);
                }
            }
        }

        private async Task ReloadAsync(CancellationToken cancellationToken)
        {
            Log(LogLevel.Debug, SecretsManagerLogEvents.ReloadStarted, "Secrets Manager configuration reload started.");
            var oldValues = _loadedValues;
            var newValues = _options.UseBatchFetch
                ? await FetchConfigurationBatchAsync(cancellationToken).ConfigureAwait(false)
                : await FetchConfigurationAsync(cancellationToken).ConfigureAwait(false);

            if (!SecretsManagerHelpers.DictionaryEquals(oldValues, newValues))
            {
                _loadedValues = newValues;
                SetData(_loadedValues, triggerReload: true);
            }
            Log(LogLevel.Debug, SecretsManagerLogEvents.ReloadCompleted, "Secrets Manager configuration reload completed.");
        }

        private void SetData(Dictionary<string, string?> values, bool triggerReload)
        {
            Data = values;
            if (triggerReload) OnReload();
        }

        private void ApplyEntry(Dictionary<string, string?> dict, string key, string value)
        {
            switch (_options.DuplicateKeyHandling)
            {
                case DuplicateKeyHandling.Throw:
                    if (dict.ContainsKey(key))
                        throw new InvalidOperationException(
                            $"Duplicate configuration key '{key}' found in AWS Secrets Manager. " +
                            "Set DuplicateKeyHandling to FirstWins or LastWins to suppress this error.");
                    dict[key] = value;
                    break;
                case DuplicateKeyHandling.FirstWins:
                    if (!dict.ContainsKey(key))
                        dict[key] = value;
                    else
                        Log(LogLevel.Debug, SecretsManagerLogEvents.DuplicateKeyResolved,
                            "Duplicate configuration key {Key} resolved (FirstWins); existing value kept.", args: key);
                    break;
                case DuplicateKeyHandling.LastWins:
                default:
                    if (dict.ContainsKey(key))
                        Log(LogLevel.Debug, SecretsManagerLogEvents.DuplicateKeyResolved,
                            "Duplicate configuration key {Key} resolved (LastWins); value overwritten.", args: key);
                    dict[key] = value;
                    break;
            }
        }

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
            var filtered = secrets.Where(_options.SecretFilter).ToList();
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

        public void Dispose()
        {
            var cancellationTokenSource = _cancellationTokenSource;
            var pollingTask = _pollingTask;
            _cancellationTokenSource = null;
            _pollingTask = null;
            cancellationTokenSource?.Cancel();
            try { pollingTask?.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
            finally { cancellationTokenSource?.Dispose(); }
        }
    }
}
