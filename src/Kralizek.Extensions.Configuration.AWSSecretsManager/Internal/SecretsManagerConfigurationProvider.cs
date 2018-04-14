using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProvider : ConfigurationProvider
    {
        private readonly IAmazonSecretsManager _client;

        public SecretsManagerConfigurationProvider(IAmazonSecretsManager client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override void Load()
        {
            LoadAsync().Wait();
        }

        async Task LoadAsync()
        {
            var allSecrets = await FetchAllSecretsAsync();

            foreach (var secret in allSecrets)
            {
                var secretValue = await _client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secret.ARN });

                if (secretValue.SecretString != null)
                {
                    var obj = JObject.Parse(secretValue.SecretString);

                    var values = ExtractValues(obj as JToken, secret.Name);

                    foreach (var value in values)
                    {
                        Data[value.key] = value.value.ToString();
                    }
                }
            }
        }

        IEnumerable<(string key, string value)> ExtractValues(JToken token, string prefix)
        {
            foreach (JProperty property in token)
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
                    if (value != null)
                    {
                        yield return (key, value);
                    }
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

                var request = new ListSecretsRequest() { NextToken = nextToken };

                response = await _client.ListSecretsAsync(request);

                result.AddRange(response.SecretList);

            } while (response.NextToken != null);

            return result;
        }

    }

}