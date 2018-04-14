using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public IAmazonSecretsManager Client { get; }

        public SecretsManagerConfigurationProvider(IAmazonSecretsManager client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
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
                var secretValue = await Client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secret.ARN });

                var secretString = secretValue.SecretString;

                if (secretString != null)
                {
                    if (IsJson(secretString))
                    {
                        var obj = JObject.Parse(secretString);

                        var values = ExtractValues(obj, secret.Name);

                        foreach (var value in values)
                        {
                            Set(value.key, value.value);
                        }
                    }
                    else
                    {
                        Set(secret.Name, secretString);
                    }
                }
            }
        }

        private static bool IsJson(string str) => str.StartsWith("[") || str.StartsWith("{");

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

                response = await Client.ListSecretsAsync(request);

                result.AddRange(response.SecretList);

            } while (response.NextToken != null);

            return result;
        }

    }

}