using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        public SecretsManagerConfigurationProviderOptions Options { get; }

        public IAmazonSecretsManager Client { get; }

        private HashSet<(string, string)> _loadedValues = new();
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationToken;

        public SecretsManagerConfigurationProvider(IAmazonSecretsManager client, SecretsManagerConfigurationProviderOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            Client = client ?? throw new ArgumentNullException(nameof(client));
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

            if (Options.PollingInterval.HasValue)
            {
                _cancellationToken = new CancellationTokenSource();
                _pollingTask = PollForChangesAsync(Options.PollingInterval.Value, _cancellationToken.Token);
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

        private static bool TryParseJson(string data, out JToken? jToken)
        {
            jToken = null;

            data = data.TrimStart();
            var firstChar = data.FirstOrDefault();

            if (firstChar != '[' && firstChar != '{')
            {
                return false;
            }

            try
            {
                jToken = JToken.Parse(data);

                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        private static IEnumerable<(string key, string value)> ExtractValues(JToken token, string prefix)
        {
            switch (token)
            {
                case JArray array:
                {
                    for (var i = 0; i < array.Count; i++)
                    {
                        var secretKey = $"{prefix}{ConfigurationPath.KeyDelimiter}{i}";
                        foreach (var (key, value) in ExtractValues(array[i], secretKey))
                        {
                            yield return (key, value);
                        }
                    }

                    break;
                }
                case JObject jObject:
                {
                    foreach (var property in jObject.Properties())
                    {
                        var secretKey = $"{prefix}{ConfigurationPath.KeyDelimiter}{property.Name}";

                        if (property.Value.HasValues)
                        {
                            foreach (var (key, value) in ExtractValues(property.Value, secretKey))
                            {
                                yield return (key, value);
                            }
                        }
                        else
                        {
                            var value = property.Value.ToString();
                            yield return (secretKey, value);
                        }
                    }

                    break;
                }
                case JValue jValue:
                {
                    var value = jValue.Value.ToString();
                    yield return (prefix, value);
                    break;
                }
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

            if (Options.AcceptedSecretArns.Count > 0)
            {
                return Options.AcceptedSecretArns.Select(x => new SecretListEntry{ARN = x, Name = x}).ToList();
            }

            var result = new List<SecretListEntry>();

            do
            {
                var nextToken = response?.NextToken;

                var request = new ListSecretsRequest {NextToken = nextToken, Filters = Options.ListSecretsFilters};

                response = await Client.ListSecretsAsync(request, cancellationToken).ConfigureAwait(false);

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
                    if (!Options.SecretFilter(secret)) continue;

                    var secretValue = await Client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secret.ARN }, cancellationToken).ConfigureAwait(false);

                    var secretEntry = Options.AcceptedSecretArns.Count > 0
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
                    
                    if (TryParseJson(secretString, out var jToken))
                    {
                        // [MaybeNullWhen(false)] attribute is available in .net standard since version 2.1
                        var values = ExtractValues(jToken, secretName);

                        foreach (var (key, value) in values)
                        {
                            var configurationKey = Options.KeyGenerator(secretEntry, key);
                            configuration.Add((configurationKey, value));
                        }
                    }
                    else
                    {
                        var configurationKey = Options.KeyGenerator(secretEntry, secretName);
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
