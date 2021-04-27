using System;
using System.Collections.Generic;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProviderOptions
    {
        public List<string> AcceptedSecretArns { get; set; } = new();
        
        public Func<SecretListEntry, bool> SecretFilter { get; set; } = _ => true;

        public List<Filter> ListSecretsFilters { get; set; } = new();

        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (secret, key) => key;

        public Action<AmazonSecretsManagerConfig> ConfigureSecretsManagerConfig { get; set; } = _ => { };

        public Func<IAmazonSecretsManager>? CreateClient { get; set; }

        public TimeSpan? PollingInterval { get; set; }
    }
}