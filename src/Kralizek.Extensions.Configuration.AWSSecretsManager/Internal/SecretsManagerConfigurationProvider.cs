using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProvider : ConfigurationProvider
    {
        public SecretsManagerConfigurationProviderOptions Options { get; }

        public IAmazonSecretsManager Client { get; }

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
            var allSecrets = await FetchAllSecretsAsync().ConfigureAwait(false);

            foreach (var secret in allSecrets)
            {
                if (!Options.SecretFilter(secret)) continue;

                var secretValue = await Client.GetSecretValueAsync(new GetSecretValueRequest {SecretId = secret.ARN}).ConfigureAwait(false);

                var secretString = secretValue.SecretString;

                if (secretString != null)
                {
                    if (IsJson(secretString))
                    {
                        var obj = JObject.Parse(secretString);

                        var values = ExtractValues(obj, secret.Name);

                        foreach (var value in values)
                        {
                            var key = Options.KeyGenerator(secret, value.key);
                            Set(key, value.value);
                        }
                    }
                    else
                    {
                        var key = Options.KeyGenerator(secret, secret.Name);
                        Set(key, secretString);
                    }
                }
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

        async Task<IReadOnlyList<SecretListEntry>> FetchAllSecretsAsync()
        {
            var response = default(ListSecretsResponse);

            var result = new List<SecretListEntry>();

            do
            {
                var nextToken = response?.NextToken;

                var request = new ListSecretsRequest() {NextToken = nextToken};

                response = await Client.ListSecretsAsync(request).ConfigureAwait(false);

                result.AddRange(response.SecretList);
            } while (response.NextToken != null);

            return result;
        }
    }
}