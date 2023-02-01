using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


namespace Kralizek.Extensions.Configuration.Internal
{
    public sealed class SecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly SecretsManagerConfigurationProviderOptions _options;
        private readonly Lazy<IAmazonSecretsManager> _client;

        private HashSet<(string, string)> _loadedValues = new();
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationToken;

        public SecretsManagerConfigurationProvider(SecretsManagerConfigurationProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _client = new Lazy<IAmazonSecretsManager>(() => CreateClient(options));
        }

        public override void Load()
        {
            LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task ForceReloadAsync(CancellationToken cancellationToken)
        {
            return ReloadAsync(cancellationToken);
        }

        private async Task LoadAsync()
        {
            _loadedValues = await FetchConfigurationAsync(default).ConfigureAwait(false);
            SetData(_loadedValues, triggerReload: false);

            if (_options.SecretsManagerConfiguration.PollingInterval.HasValue)
            {
                _cancellationToken = new CancellationTokenSource();
                _pollingTask = PollForChangesAsync(_options.SecretsManagerConfiguration.PollingInterval.Value, _cancellationToken.Token);
            }
        }

        private async Task PollForChangesAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                try
                {
                    await ReloadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }
        }

        private async Task ReloadAsync(CancellationToken cancellationToken)
        {
            var oldValues = _loadedValues;
            var newValues = await FetchConfigurationAsync(cancellationToken).ConfigureAwait(false);

            if (!oldValues.SetEquals(newValues))
            {
                _loadedValues = newValues;
                SetData(_loadedValues, triggerReload: true);
            }
        }

        private static bool TryParseJson(string data, out JsonElement? jsonElement)
        {
            jsonElement = null;

            data = data.TrimStart();
            var firstChar = data.FirstOrDefault();

            if (firstChar != '[' && firstChar != '{')
            {
                return false;
            }

            try
            {
                using var jsonDocument = JsonDocument.Parse(data);
                //  https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-use-dom-utf8jsonreader-utf8jsonwriter?pivots=dotnet-6-0#jsondocument-is-idisposable
                //  Its recommended to return the clone of the root element as the json document will be disposed
                jsonElement = jsonDocument.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static IEnumerable<(string key, string value)> ExtractValues(JsonElement? jsonElement, string prefix)
        {
            if (jsonElement == null)
            {
                yield break;
            }
            var element = jsonElement.Value;
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    var currentIndex = 0;
                    foreach (var el in element.EnumerateArray())
                    {
                        var secretKey = $"{prefix}{ConfigurationPath.KeyDelimiter}{currentIndex}";
                        foreach (var (key, value) in ExtractValues(el, secretKey))
                        {
                            yield return (key, value);
                        }
                        currentIndex++;
                    }
                    break;
                }
                case JsonValueKind.Number:
                {
                    var value = element.GetRawText();
                    yield return (prefix, value);
                    break;
                }
                case JsonValueKind.String:
                {
                    var value = element.GetString() ?? "";
                    yield return (prefix, value);
                    break;
                }
                case JsonValueKind.True:
                {
                    var value = element.GetBoolean();
                    yield return (prefix, value.ToString());
                    break;
                }
                case JsonValueKind.False:
                {
                    var value = element.GetBoolean();
                    yield return (prefix, value.ToString());
                    break;
                }
                case JsonValueKind.Object:
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        var secretKey = $"{prefix}{ConfigurationPath.KeyDelimiter}{property.Name}";
                        foreach (var (key, value) in ExtractValues(property.Value, secretKey))
                        {
                            yield return (key, value);
                        }
                    }
                    break;
                }
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                default:
                {
                    throw new FormatException("unsupported json token");
                }
            }
        }

        private void SetData(IEnumerable<(string, string)> values, bool triggerReload)
        {
            Data = values.ToDictionary(x => x.Item1, x => x.Item2, StringComparer.InvariantCultureIgnoreCase);
            if (triggerReload)
            {
                OnReload();
            }
        }

        private async Task<IReadOnlyList<SecretListEntry>> FetchAllSecretsAsync(CancellationToken cancellationToken)
        {
            var response = default(ListSecretsResponse);

            if (_options.SecretsManagerConfiguration.AcceptedSecretArns.Count > 0)
            {
                return _options.SecretsManagerConfiguration.AcceptedSecretArns.Select(x => new SecretListEntry{ARN = x, Name = x}).ToList();
            }

            var result = new List<SecretListEntry>();

            do
            {
                var nextToken = response?.NextToken;

                var request = new ListSecretsRequest {NextToken = nextToken, Filters = _options.SecretsManagerConfiguration.ListSecretsFilters};

                response = await _client.Value.ListSecretsAsync(request, cancellationToken).ConfigureAwait(false);

                result.AddRange(response.SecretList);
            } while (response.NextToken != null);

            return result;
        }

        private async Task<HashSet<(string, string)>> FetchConfigurationAsync(CancellationToken cancellationToken)
        {
            var secrets = await FetchAllSecretsAsync(cancellationToken).ConfigureAwait(false);
            var configuration = new HashSet<(string, string)>();
            foreach (var secret in secrets)
            {
                try
                {
                    if (!_options.SecretsManagerConfiguration.SecretFilter(secret)) continue;

                    var request = new GetSecretValueRequest { SecretId = secret.ARN };
                    _options.SecretsManagerConfiguration.ConfigureSecretValueRequest?.Invoke(request, new SecretValueContext(secret));
                    var secretValue = await _client.Value.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);

                    var secretEntry = _options.SecretsManagerConfiguration.AcceptedSecretArns.Count > 0
                        ? new SecretListEntry
                        {
                            ARN = secret.ARN,
                            Name = secretValue.Name,
                            CreatedDate = secretValue.CreatedDate
                        }
                        : secret;

                    var secretName = secretEntry.Name;
                    var secretString = secretValue.SecretString;

                    if (secretString is null)
                        continue;

                    if (TryParseJson(secretString, out var jElement))
                    {
                        // [MaybeNullWhen(false)] attribute is available in .net standard since version 2.1
                        var values = ExtractValues(jElement!, secretName);

                        foreach (var (key, value) in values)
                        {
                            var configurationKey = _options.SecretsManagerConfiguration.KeyGenerator(secretEntry, key);
                            configuration.Add((configurationKey, value));
                        }
                    }
                    else
                    {
                        var configurationKey = _options.SecretsManagerConfiguration.KeyGenerator(secretEntry, secretName);
                        configuration.Add((configurationKey, secretString));
                    }
                }
                catch (ResourceNotFoundException e)
                {
                    throw new MissingSecretValueException($"Error retrieving secret value (Secret: {secret.Name} Arn: {secret.ARN})", secret.Name, secret.ARN, e);
                }
            }
            return configuration;
        }
        
        private static IAmazonSecretsManager CreateClient(SecretsManagerConfigurationProviderOptions options)
        {
            if (options.SecretsManagerConfiguration.ClientFactory != null)
            {
                return options.SecretsManagerConfiguration.ClientFactory();
            }
            
            options.ConfigureClient?.Invoke(options.AWSOptions.DefaultClientConfig);

            var client = options.AWSOptions.CreateServiceClient<IAmazonSecretsManager>();

            return client;
        }

        public void Dispose()
        {
            _cancellationToken?.Cancel();
            _cancellationToken = null;

            try
            {
                _pollingTask?.GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
            }
            _pollingTask = null;
        }
    }
}
