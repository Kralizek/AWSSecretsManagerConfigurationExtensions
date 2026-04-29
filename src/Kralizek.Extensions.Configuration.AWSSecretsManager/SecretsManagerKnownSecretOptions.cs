using System;

using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Options for the known-single-secret AWS Secrets Manager configuration provider.
    /// This provider fetches exactly one configured secret id/name/ARN using <c>GetSecretValue</c>.
    /// It never calls <c>ListSecrets</c> or <c>BatchGetSecretValue</c>.
    /// <para>
    /// Required IAM permissions:
    /// <list type="bullet">
    ///   <item><description><c>secretsmanager:GetSecretValue</c> for the configured secret.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class SecretsManagerKnownSecretOptions
    {
        /// <summary>
        /// Gets or sets a function that maps a <see cref="SecretListEntry"/> and raw configuration key
        /// to the final configuration key stored in the provider.
        /// </summary>
        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (_, key) => key;

        /// <summary>
        /// Gets or sets the policy applied when two keys from within the same secret
        /// produce the same configuration key after key generation.
        /// Defaults to <see cref="DuplicateKeyHandling.LastWins"/>.
        /// </summary>
        public DuplicateKeyHandling DuplicateKeyHandling { get; set; } = DuplicateKeyHandling.LastWins;

        /// <summary>
        /// Gets or sets an optional callback invoked before the <c>GetSecretValue</c> request.
        /// </summary>
        public Action<GetSecretValueRequest, SecretValueContext>? ConfigureSecretValueRequest { get; set; }

        /// <summary>
        /// Gets or sets the interval at which the provider polls AWS for an updated secret value.
        /// When <see langword="null"/>, polling is disabled and the secret is loaded only once at startup.
        /// </summary>
        public TimeSpan? ReloadInterval { get; set; }

        /// <summary>
        /// Gets or sets the sink that receives structured log events from this provider.
        /// Use <see cref="SecretsManagerLogging.ToLogEventSink"/> to bridge to an <c>ILogger</c>.
        /// </summary>
        public Action<SecretsManagerLogEvent>? LogEvent { get; set; }
    }
}