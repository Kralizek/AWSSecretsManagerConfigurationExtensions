using System;
using System.Collections.Generic;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Options for the known-multiple-secrets AWS Secrets Manager configuration provider.
    /// This provider fetches the configured known secret ids/names/ARNs.
    /// It never calls <c>ListSecrets</c>. It uses <c>BatchGetSecretValue</c> by default,
    /// or <c>GetSecretValue</c> per configured secret when <see cref="UseBatchFetch"/> is disabled.
    /// <para>
    /// Required IAM permissions:
    /// <list type="bullet">
    ///   <item><description><c>secretsmanager:BatchGetSecretValue</c> by default.</description></item>
    ///   <item><description><c>secretsmanager:GetSecretValue</c> when <see cref="UseBatchFetch"/> is <see langword="false"/>.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class SecretsManagerKnownSecretsOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use <c>BatchGetSecretValue</c>
        /// (the default) or individual <c>GetSecretValue</c> calls per configured secret.
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
