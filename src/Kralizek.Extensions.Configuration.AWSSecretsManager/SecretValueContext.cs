using System.Collections.Generic;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration
{
    public class SecretValueContext
    {
        public SecretValueContext(SecretListEntry secret)
        {
            _ = secret ?? throw new System.ArgumentNullException(nameof(secret));
            Name = secret.Name;
            VersionsToStages = secret.SecretVersionsToStages;
        }

        public string Name { get; }
        public Dictionary<string, List<string>> VersionsToStages { get; }
    }
}
