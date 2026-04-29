using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// Configuration provider that fetches exactly one known secret using <c>GetSecretValue</c>.
    /// Never calls <c>ListSecrets</c> or <c>BatchGetSecretValue</c>.
    /// </summary>
    public sealed class SecretsManagerKnownSecretConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IAmazonSecretsManager _client;
        private readonly string _secretId;
        private readonly SecretsManagerKnownSecretOptions _options;

        private Dictionary<string, string?> _loadedValues = new(StringComparer.InvariantCultureIgnoreCase);
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public SecretsManagerKnownSecretConfigurationProvider(IAmazonSecretsManager client, string secretId, SecretsManagerKnownSecretOptions options)
        {
            _client   = client   ?? throw new ArgumentNullException(nameof(client));
            _secretId = !string.IsNullOrWhiteSpace(secretId) ? secretId : throw new ArgumentException("Secret id must not be null or whitespace.", nameof(secretId));
            _options  = options  ?? throw new ArgumentNullException(nameof(options));
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        public Task ForceReloadAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

        private void Log(LogLevel level, EventId eventId, string message, Exception? ex = null, params object?[] args)
            => _options.LogEvent?.Invoke(new SecretsManagerLogEvent(level, eventId, message, ex, Args: args));

        private async Task LoadAsync()
        {
            Log(LogLevel.Debug, SecretsManagerLogEvents.LoadStarted, "Secrets Manager configuration load started.");

            _loadedValues = await FetchConfigurationAsync(default).ConfigureAwait(false);
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
            var newValues = await FetchConfigurationAsync(cancellationToken).ConfigureAwait(false);

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

            var request = new GetSecretValueRequest { SecretId = _secretId };
            // Use a synthetic SecretListEntry for the context (no ARN/Name yet before fetch)
            var syntheticEntry = new SecretListEntry { ARN = _secretId, Name = _secretId };
            _options.ConfigureSecretValueRequest?.Invoke(request, new SecretValueContext(syntheticEntry));

            GetSecretValueResponse secretValue;
            try
            {
                secretValue = await _client.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (ResourceNotFoundException e)
            {
                throw new MissingSecretValueException(
                    $"Error retrieving secret value (SecretId: {_secretId})",
                    _secretId, _secretId, e);
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
