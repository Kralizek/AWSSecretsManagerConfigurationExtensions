using System;
using System.Collections.Generic;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// A secret context when making a request to get a secret value.
    /// </summary>
    public class SecretValueContext
    {
        public SecretValueContext(SecretListEntry secret)
        {
            _ = secret ?? throw new ArgumentNullException(nameof(secret));

            Name = secret.Name;
            VersionsToStages = secret.SecretVersionsToStages;
        }

        /// <summary>
        /// The secret name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A list of all the secret's currently assigned SecretVersionStage staging levels and SecretVersionId attached to each one.
        /// </summary>
        public Dictionary<string, List<string>> VersionsToStages { get; }
    }
}