using System;
using System.Collections.Generic;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class SecretsManagerConfigurationProviderOptions
    {
        public bool UseDefinedArnList { get; set; } = false;
        public List<string> DefinedSecretArns { get; set; } = new List<string>();
        public Func<SecretListEntry, bool> SecretFilter { get; set; } = _ => true;

        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (secret, key) => key;

        public Action<AmazonSecretsManagerConfig> ConfigureSecretsManagerConfig { get; set; } = _ => { };

        public Func<IAmazonSecretsManager>? CreateClient { get; set; }

        public TimeSpan? PollingInterval { get; set; }
    }
}