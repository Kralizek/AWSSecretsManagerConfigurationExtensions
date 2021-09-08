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

        /// <summary>
        /// Determines whether or not the options for the configuration provider should be read from configuration.
        /// </summary>
        public bool ReadFromConfig { get; set; }
        
        /// <summary>
        /// The name of the configuration section containing the options.
        /// </summary>
        public string ConfigSectionName { get; set; } = SecretsManagerConfigurationSection.DefaultConfigSectionName;
        
        /// <summary>
        /// Enables the extension to read options from an IConfigurationSection.
        /// </summary>
        /// <param name="configSectionName">The name of the configuration section containing the options.</param>
        public void ReadFromConfigSection(string configSectionName = SecretsManagerConfigurationSection.DefaultConfigSectionName)
        {
            ReadFromConfig = true;
            ConfigSectionName = configSectionName;
        }
    }
}