using System;
using System.Collections.Generic;

using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Options for the discovery-based AWS Secrets Manager configuration provider.
    /// This provider discovers secrets by calling <c>ListSecrets</c>.
    /// It then fetches discovered values using <c>BatchGetSecretValue</c> by default,
    /// or <c>GetSecretValue</c> when <see cref="UseBatchFetch"/> is disabled.
    /// </summary>
    public sealed class SecretsManagerDiscoveryOptions
    {
        /// <summary>
        /// Gets or sets a predicate used to filter secrets returned by <c>ListSecrets</c>.
        /// Only secrets for which this returns <see langword="true"/> are fetched.
        /// Defaults to accepting all secrets.
        /// </summary>
        public Func<SecretListEntry, bool> SecretFilter { get; set; } = _ => true;

        /// <summary>
        /// Gets the server-side filters applied to the <c>ListSecrets</c> API call.
        /// These reduce the result set at the server but do not remove the requirement for
        /// the <c>secretsmanager:ListSecrets</c> IAM permission.
        /// </summary>
        public List<Filter> ListSecretsFilters { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether to use <c>BatchGetSecretValue</c>
        /// (the default) or fall back to individual <c>GetSecretValue</c> calls per discovered secret.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool UseBatchFetch { get; set; } = true;

        /// <summary>
        /// Gets or sets a function that maps a <see cref="SecretListEntry"/> and raw configuration key
        /// to the final configuration key stored in the provider.
        /// </summary>
        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (_, key) => key;

        /// <summary>
        /// Gets or sets the policy applied when two secrets produce the same configuration key.
        /// Defaults to <see cref="DuplicateKeyHandling.LastWins"/>.
        /// </summary>
        public DuplicateKeyHandling DuplicateKeyHandling { get; set; } = DuplicateKeyHandling.LastWins;

        /// <summary>
        /// Gets or sets an optional callback invoked before each <c>GetSecretValue</c> request
        /// (when <see cref="UseBatchFetch"/> is <see langword="false"/>).
        /// </summary>
        public Action<GetSecretValueRequest, SecretValueContext>? ConfigureSecretValueRequest { get; set; }

        /// <summary>
        /// Gets or sets an optional callback invoked before each <c>BatchGetSecretValue</c> request
        /// (when <see cref="UseBatchFetch"/> is <see langword="true"/>).
        /// </summary>
        public Action<BatchGetSecretValueRequest, IReadOnlyList<SecretValueContext>>? ConfigureBatchSecretValueRequest { get; set; }

        /// <summary>
        /// Gets or sets the interval at which the provider polls AWS for updated secret values.
        /// When <see langword="null"/>, polling is disabled and secrets are loaded only once at startup.
        /// </summary>
        public TimeSpan? ReloadInterval { get; set; }

        /// <summary>
        /// Gets or sets the sink that receives structured log events from this provider.
        /// Use <see cref="SecretsManagerLogging.ToLogEventSink"/> to bridge to an <c>ILogger</c>.
        /// </summary>
        public Action<SecretsManagerLogEvent>? LogEvent { get; set; }
    }
}