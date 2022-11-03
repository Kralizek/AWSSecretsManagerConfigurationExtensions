using System.Collections.Generic;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// An object intended to bind an <see cref="IConfigurationSection"/> to.
    /// </summary>
    internal class SecretsManagerConfigurationSection
    {
        public const string DefaultConfigSectionName = "SecretsManager";
        
        /// <summary>
        /// The string representation of an AWS Region.
        /// </summary>
        public string? Region { get; set; }
        
        /// <summary>
        /// The name of the credentials profile to be used by the Secrets Manager client.
        /// </summary>
        public string? Profile { get; set; }
        
        /// <summary>
        /// The location of the file containing the credentials profiles.
        /// </summary>
        public string? ProfilesLocation { get; set; }
        
        /// <summary>
        /// The duration in sections that should be waited before refreshing the secrets.
        /// </summary>
        public int? PollingIntervalInSeconds { get; set; }
        
        /// <summary>
        /// A list of identifiers for the secrets that are to be retrieved.
        /// The secret ARN (full or partial) and secret name are supported.
        /// </summary>
        public List<string>? AcceptedSecretArns { get; set; }
        
        /// <summary>
        /// A list of filters that get passed to the client to filter the listed secrets before returning them. 
        /// </summary>
        public List<SecretFilter>? ListSecretsFilters { get; set; }

        /// <summary>
        /// Class used to bind configuration to. 
        /// Unable to bind to the AWS <see cref="Filter"/> class due to issues binding to the <see cref="Filter.Key"/> property.
        /// </summary>
        internal class SecretFilter
        {
            public string Key { get; set; } = null!;
            public List<string> Values { get; set; } = null!;

            /// <summary>
            /// Converts this instance to a <see cref="Filter"/> instance.
            /// </summary>
            /// <returns></returns>
            public Filter ToAwsFilter()
                => new Filter
                {
                    Key = this.Key,
                    Values = this.Values
                };
        }
    }
}