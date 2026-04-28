using System;
using System.Collections.Generic;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration
{
    public class SecretsManagerOptions
    {
        public List<string> SecretIds { get; } = new();
        public Func<SecretListEntry, bool> SecretFilter { get; set; } = _ => true;
        public List<Filter> ListSecretsFilters { get; } = new();
        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (_, key) => key;
        public Action<GetSecretValueRequest, SecretValueContext>? ConfigureSecretValueRequest { get; set; }
        public Action<BatchGetSecretValueRequest, IReadOnlyList<SecretValueContext>>? ConfigureBatchSecretValueRequest { get; set; }
        public TimeSpan? ReloadInterval { get; set; }
        public bool UseBatchFetch { get; set; }
        public bool IgnoreMissingValues { get; set; }
        public DuplicateKeyHandling DuplicateKeyHandling { get; set; } = DuplicateKeyHandling.LastWins;
        public Action<SecretsManagerLogEvent>? LogEvent { get; set; }
    }
}
