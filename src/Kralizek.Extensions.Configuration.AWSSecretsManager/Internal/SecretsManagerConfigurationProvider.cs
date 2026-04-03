using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IAmazonSecretsManager _client;
        private readonly SecretsManagerOptions _options;

        private HashSet<(string, string)> _loadedValues = new HashSet<(string, string)>();
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public SecretsManagerConfigurationProvider(IAmazonSecretsManager client, SecretsManagerOptions options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override void Load()
        {
            LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task ForceReloadAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

        private void Log(LogLevel level, EventId eventId, string message,
            Exception? ex = null, params object?[] args)
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
                try
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                try
                {
                    await ReloadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Log(LogLevel.Error, SecretsManagerLogEvents.ReloadFailed,
                        "Secrets Manager configuration reload failed.", ex);
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

            if (!oldValues.SetEquals(newValues))
            {
                _loadedValues = newValues;
                SetData(_loadedValues, triggerReload: true);
            }

            Log(LogLevel.Debug, SecretsManagerLogEvents.ReloadCompleted, "Secrets Manager configuration reload completed.");
        }

        private static bool TryParseJson(string data, out JsonElement? jsonElement)
        {
            jsonElement = null;
            data = data.TrimStart();
            var firstChar = data.FirstOrDefault();
            if (firstChar != '[' && firstChar != '{') return false;
            try
            {
                using var doc = JsonDocument.Parse(data);
                jsonElement = doc.RootElement.Clone();
                return true;
            }
            catch (JsonException) { return false; }
        }

        private static IEnumerable<(string key, string value)> ExtractValues(JsonElement? jsonElement, string prefix)
        {
            if (jsonElement == null) yield break;
            var element = jsonElement.Value;
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    var i = 0;
                    foreach (var el in element.EnumerateArray())
                    {
                        var k = $"{prefix}{ConfigurationPath.KeyDelimiter}{i++}";
                        foreach (var pair in ExtractValues(el, k)) yield return pair;
                    }
                    break;
                }
                case JsonValueKind.Number:
                    yield return (prefix, element.GetRawText());
                    break;
                case JsonValueKind.String:
                    yield return (prefix, element.GetString() ?? "");
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    yield return (prefix, element.GetBoolean().ToString());
                    break;
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var k = $"{prefix}{ConfigurationPath.KeyDelimiter}{prop.Name}";
                        foreach (var pair in ExtractValues(prop.Value, k)) yield return pair;
                    }
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                default:
                    throw new FormatException("unsupported json token");
            }
        }

        private void SetData(IEnumerable<(string, string)> values, bool triggerReload)
        {
            Data = values.ToDictionary<(string, string), string, string?>(
                x => x.Item1, x => x.Item2, StringComparer.InvariantCultureIgnoreCase);
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
            if (_options.SecretIds.Count > 0)
                return _options.SecretIds.Select(id => new SecretListEntry { ARN = id, Name = id }).ToList();

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

        private async Task<HashSet<(string, string)>> FetchConfigurationAsync(CancellationToken cancellationToken)
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
                    catch (ResourceNotFoundException) when (_options.IgnoreMissingValues)
                    {
                        Log(LogLevel.Debug, SecretsManagerLogEvents.MissingSecretIgnored,
                            "Missing secret {SecretName} ignored.", args: secret.Name);
                        continue;
                    }

                    var secretEntry = _options.SecretIds.Count > 0
                        ? new SecretListEntry { ARN = secret.ARN, Name = secretValue.Name, CreatedDate = secretValue.CreatedDate }
                        : secret;

                    var secretString = secretValue.SecretString;
                    if (secretString is null) continue;

                    if (TryParseJson(secretString, out var jElement))
                    {
                        foreach (var (key, value) in ExtractValues(jElement!, secretEntry.Name))
                        {
                            var configKey = _options.KeyGenerator(secretEntry, key);
                            ApplyEntry(dict, configKey, value);
                        }
                    }
                    else
                    {
                        var configKey = _options.KeyGenerator(secretEntry, secretEntry.Name);
                        ApplyEntry(dict, configKey, secretString);
                    }

                    Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded.",
                        args: secretEntry.Name);
                }
                catch (ResourceNotFoundException e)
                {
                    throw new MissingSecretValueException(
                        $"Error retrieving secret value (Secret: {secret.Name} Arn: {secret.ARN})",
                        secret.Name, secret.ARN, e);
                }
            }

            return new HashSet<(string, string)>(dict.Select(kv => (kv.Key, kv.Value ?? "")));
        }

        private static List<List<SecretListEntry>> ChunkList(IReadOnlyList<SecretListEntry> source,
            Func<SecretListEntry, bool> filter, int chunkSize)
            => source.Where(filter)
                .Select((item, idx) => (item, idx))
                .GroupBy(x => x.idx / chunkSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

        private async Task<HashSet<(string, string)>> FetchConfigurationBatchAsync(CancellationToken cancellationToken)
        {
            var secrets = await FetchAllSecretsAsync(cancellationToken).ConfigureAwait(false);
            var dict = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);
            var chunked = ChunkList(secrets, _options.SecretFilter, 20);

            foreach (var secretSet in chunked)
            {
                var request = new BatchGetSecretValueRequest
                {
                    SecretIdList = secretSet.Select(a => a.ARN).ToList()
                };
                var contextList = secretSet.Select(a => new SecretValueContext(a)).ToList();
                _options.ConfigureBatchSecretValueRequest?.Invoke(request,
                    (IReadOnlyList<SecretValueContext>)contextList);

                try
                {
                    BatchGetSecretValueResponse? secretValueSet = null;
                    do
                    {
                        request.NextToken = secretValueSet?.NextToken;
                        secretValueSet = await _client.BatchGetSecretValueAsync(request, cancellationToken)
                            .ConfigureAwait(false);

                        if (secretValueSet.Errors?.Any() == true)
                        {
                            var errors = HandleBatchErrors(secretValueSet);
                            if (!_options.IgnoreMissingValues || errors.Any(e => e is not MissingSecretValueException))
                                throw new AggregateException(errors);

                            foreach (var missingEx in errors.OfType<MissingSecretValueException>())
                                Log(LogLevel.Debug, SecretsManagerLogEvents.MissingSecretIgnored,
                                    "Missing secret {SecretName} ignored (batch).", args: missingEx.SecretName);
                        }

                        foreach (var (secretValue, secret) in secretValueSet.SecretValues
                            .Join(secretSet, sv => sv.ARN, s => s.ARN, (sv, s) => (sv, s)))
                        {
                            var secretEntry = _options.SecretIds.Count > 0
                                ? new SecretListEntry { ARN = secret.ARN, Name = secretValue.Name, CreatedDate = secretValue.CreatedDate }
                                : secret;

                            var secretString = secretValue.SecretString;
                            if (secretString is null) continue;

                            if (TryParseJson(secretString, out var jElement))
                            {
                                foreach (var (key, value) in ExtractValues(jElement!, secretEntry.Name))
                                {
                                    var configKey = _options.KeyGenerator(secretEntry, key);
                                    ApplyEntry(dict, configKey, value);
                                }
                            }
                            else
                            {
                                var configKey = _options.KeyGenerator(secretEntry, secretEntry.Name);
                                ApplyEntry(dict, configKey, secretString);
                            }

                            Log(LogLevel.Debug, SecretsManagerLogEvents.SecretLoaded, "Secret {SecretName} loaded (batch).",
                                args: secretEntry.Name);
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

            return new HashSet<(string, string)>(dict.Select(kv => (kv.Key, kv.Value ?? "")));
        }

        private static List<Exception> HandleBatchErrors(BatchGetSecretValueResponse response)
        {
            return response.Errors.Select<APIErrorType, Exception>(err => err.ErrorCode switch
            {
                "DecryptionFailure" => new DecryptionFailureException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "InternalServiceError" => new InternalServiceErrorException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "InvalidParameterException" => new InvalidParameterException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "InvalidRequestException" => new InvalidRequestException(err.Message, ErrorType.Unknown,
                    err.ErrorCode, response.ResponseMetadata.RequestId, response.HttpStatusCode),
                "ResourceNotFoundException" => new MissingSecretValueException(err.Message, err.SecretId, err.SecretId,
                    new ResourceNotFoundException(err.Message, ErrorType.Unknown, err.ErrorCode,
                        response.ResponseMetadata.RequestId, response.HttpStatusCode)),
                _ => new AmazonServiceException(err.Message, ErrorType.Unknown, err.ErrorCode,
                    response.ResponseMetadata.RequestId, response.HttpStatusCode)
            }).ToList();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            try { _pollingTask?.GetAwaiter().GetResult(); } catch (TaskCanceledException) { }
            _pollingTask = null;
        }
    }
}
