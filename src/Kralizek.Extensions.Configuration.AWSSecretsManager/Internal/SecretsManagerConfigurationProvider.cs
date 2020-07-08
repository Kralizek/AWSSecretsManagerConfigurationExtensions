using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        public SecretsManagerConfigurationProviderOptions Options { get; }

        public IAmazonSecretsManager Client { get; }

        private HashSet<(string, string)> _loadedValues;
        private Task _pollingTask;
        private CancellationTokenSource _cancellationToken;

        public SecretsManagerConfigurationProvider(IAmazonSecretsManager client, SecretsManagerConfigurationProviderOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override void Load()
        {
            LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task LoadAsync()
        {
            _loadedValues = await FetchConfigurationAsync(default(CancellationToken)).ConfigureAwait(false);
            SetData(_loadedValues, triggerReload: false);

            if (Options.PollingInterval.HasValue)
            {
                _cancellationToken = new CancellationTokenSource();
                _pollingTask = PollForChangesAsync(Options.PollingInterval.Value, _cancellationToken.Token);
            }
        }

        async Task PollForChangesAsync(TimeSpan interval, CancellationToken cancellationToken)
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

        async Task ReloadAsync(CancellationToken cancellationToken)
        {
            var oldValues = _loadedValues;
            var newValues = await FetchConfigurationAsync(cancellationToken).ConfigureAwait(false);

            if (!oldValues.SetEquals(newValues))
            {
                _loadedValues = newValues;
                SetData(_loadedValues, triggerReload: true);
            }
        }

        private static bool IsJson(string str) => str.StartsWith("[") || str.StartsWith("{");

        IEnumerable<(string key, string value)> ExtractValues(JToken token, string prefix)
        {
            switch (token)
            {
                case JArray array:
                {
                    for (var i = 0; i < array.Count; i++)
                    {
                        var key = $"{prefix}{ConfigurationPath.KeyDelimiter}{i}";
                        foreach (var item in ExtractValues(array[i], key))
                        {
                            yield return (item.key, item.value);
                        }
                    }

                    break;
                }
                case JObject jObject:
                {
                    foreach (var property in jObject.Properties())
                    {
                        var key = $"{prefix}{ConfigurationPath.KeyDelimiter}{property.Name}";

                        if (property.Value.HasValues)
                        {
                            foreach (var item in ExtractValues(property.Value, key))
                            {
                                yield return (item.key, item.value);
                            }
                        }
                        else
                        {
                            var value = property.Value.ToString();
                            yield return (key, value);
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

        void SetData(IEnumerable<(string, string)> values, bool triggerReload)
        {
            Data = values.ToDictionary(x => x.Item1, x => x.Item2);
            if (triggerReload)
            {
                OnReload();
            }
        }

        async Task<IReadOnlyList<SecretListEntry>> FetchAllSecretsAsync(CancellationToken cancellationToken)
        {
            var response = default(ListSecretsResponse);

            var result = new List<SecretListEntry>();

            do
            {
                var nextToken = response?.NextToken;

                var request = new ListSecretsRequest() {NextToken = nextToken};

                response = await Client.ListSecretsAsync(request, cancellationToken).ConfigureAwait(false);

                result.AddRange(response.SecretList);
            } while (response.NextToken != null);

            return result;
        }
        
        async Task<HashSet<(string, string)>> FetchConfigurationAsync(CancellationToken cancellationToken)
        {
            var secrets = await FetchAllSecretsAsync(cancellationToken).ConfigureAwait(false);
            var configuration = new HashSet<(string, string)>();
            foreach (var secret in secrets)
            {
                try
                {
                    if (!Options.SecretFilter(secret)) continue;

                    var secretValue = await Client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secret.ARN }, cancellationToken).ConfigureAwait(false);

                    var secretString = secretValue.SecretString;

                    if (secretString != null)
                    {
                        if (IsJson(secretString))
                        {
                            var obj = JToken.Parse(secretString);

                            var values = ExtractValues(obj, secret.Name);

                            foreach (var value in values)
                            {
                                var key = Options.KeyGenerator(secret, value.key);
                                configuration.Add((key, value.value));
                            }
                        }
                        else
                        {
                            var key = Options.KeyGenerator(secret, secret.Name);
                            configuration.Add((key, secretString));
                        }
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