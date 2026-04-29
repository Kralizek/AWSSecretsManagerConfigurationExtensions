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
    /// Configuration provider that fetches a configured set of known secrets.
    /// Never calls <c>ListSecrets</c>. Uses <c>BatchGetSecretValue</c> by default.
    /// </summary>
    public sealed class SecretsManagerKnownSecretsConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IAmazonSecretsManager _client;
        private readonly IReadOnlyList<string> _secretIds;
        private readonly SecretsManagerKnownSecretsOptions _options;

        private Dictionary<string, string?> _loadedValues = new(StringComparer.InvariantCultureIgnoreCase);
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public SecretsManagerKnownSecretsConfigurationProvider(IAmazonSecretsManager client, IReadOnlyList<string> secretIds, SecretsManagerKnownSecretsOptions options)
        {
            _client    = client    ?? throw new ArgumentNullException(nameof(client));
            _secretIds = secretIds ?? throw new ArgumentNullException(nameof(secretIds));
            _options   = options   ?? throw new ArgumentNullException(nameof(options));
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
                    // Build a lookup of all responses by ARN and Name for ordered processing below
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

                    // Process in the order of configured secretIds
                    foreach (var secretId in secretIdSet)
                    {
                        if (!responseMap.TryGetValue(secretId, out var secretValue))
                            continue;

                        var rootKey = !string.IsNullOrEmpty(secretValue.Name) ? secretValue.Name : secretId;
                        var secretEntry = new SecretListEntry { ARN = secretValue.ARN, Name = rootKey };

                        var secretString = secretValue.SecretString;
                        if (secretString is null) continue;

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

                        Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded (batch).", args: rootKey);
                    }
                }
                catch (AggregateException) { throw; }
                catch (ResourceNotFoundException e)
                {
                    var names = string.Join(",", secretIdSet);
                    throw new MissingSecretValueException(
                        $"Error retrieving secret value (Secrets: {names} Arns: {names})", names, names, e);
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
