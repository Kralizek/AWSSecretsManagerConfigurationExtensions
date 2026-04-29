using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering AWS Secrets Manager configuration providers.
    /// </summary>
    public static class SecretsManagerExtensions
    {
        #region AddSecretsManagerDiscovery

        /// <summary>
        /// Adds a discovery-based AWS Secrets Manager configuration provider using ambient AWS credentials.
        /// <para>
        /// This provider discovers secrets by calling <c>ListSecrets</c>. It then fetches discovered values
        /// using <c>BatchGetSecretValue</c> by default, or <c>GetSecretValue</c> when
        /// <see cref="SecretsManagerDiscoveryOptions.UseBatchFetch"/> is disabled.
        /// </para>
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerDiscovery(
            this IConfigurationBuilder builder,
            Action<SecretsManagerDiscoveryOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            var options = new SecretsManagerDiscoveryOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerDiscoveryConfigurationSource(options));
            return builder;
        }

        /// <summary>
        /// Adds a discovery-based AWS Secrets Manager configuration provider using the supplied <see cref="AWSOptions"/>.
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerDiscovery(
            this IConfigurationBuilder builder,
            AWSOptions awsOptions,
            Action<SecretsManagerDiscoveryOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (awsOptions is null) throw new ArgumentNullException(nameof(awsOptions));
            var options = new SecretsManagerDiscoveryOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerDiscoveryConfigurationSource(awsOptions, options));
            return builder;
        }

        /// <summary>
        /// Adds a discovery-based AWS Secrets Manager configuration provider using the supplied client.
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerDiscovery(
            this IConfigurationBuilder builder,
            IAmazonSecretsManager client,
            Action<SecretsManagerDiscoveryOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (client is null) throw new ArgumentNullException(nameof(client));
            var options = new SecretsManagerDiscoveryOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerDiscoveryConfigurationSource(client, options));
            return builder;
        }

        #endregion

        #region AddSecretsManagerKnownSecret

        /// <summary>
        /// Adds a known-single-secret AWS Secrets Manager configuration provider using ambient AWS credentials.
        /// <para>
        /// This provider fetches exactly one configured secret id/name/ARN using <c>GetSecretValue</c>.
        /// It never calls <c>ListSecrets</c> or <c>BatchGetSecretValue</c>.
        /// </para>
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerKnownSecret(
            this IConfigurationBuilder builder,
            string secretId,
            Action<SecretsManagerKnownSecretOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            ValidateSecretId(secretId, nameof(secretId));
            var options = new SecretsManagerKnownSecretOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerKnownSecretConfigurationSource(secretId, options));
            return builder;
        }

        /// <summary>
        /// Adds a known-single-secret AWS Secrets Manager configuration provider using the supplied <see cref="AWSOptions"/>.
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerKnownSecret(
            this IConfigurationBuilder builder,
            AWSOptions awsOptions,
            string secretId,
            Action<SecretsManagerKnownSecretOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (awsOptions is null) throw new ArgumentNullException(nameof(awsOptions));
            ValidateSecretId(secretId, nameof(secretId));
            var options = new SecretsManagerKnownSecretOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerKnownSecretConfigurationSource(awsOptions, secretId, options));
            return builder;
        }

        /// <summary>
        /// Adds a known-single-secret AWS Secrets Manager configuration provider using the supplied client.
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerKnownSecret(
            this IConfigurationBuilder builder,
            IAmazonSecretsManager client,
            string secretId,
            Action<SecretsManagerKnownSecretOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (client is null) throw new ArgumentNullException(nameof(client));
            ValidateSecretId(secretId, nameof(secretId));
            var options = new SecretsManagerKnownSecretOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerKnownSecretConfigurationSource(client, secretId, options));
            return builder;
        }

        #endregion

        #region AddSecretsManagerKnownSecrets

        /// <summary>
        /// Adds a known-multiple-secrets AWS Secrets Manager configuration provider using ambient AWS credentials.
        /// <para>
        /// This provider fetches the configured known secret ids/names/ARNs. It never calls <c>ListSecrets</c>.
        /// It uses <c>BatchGetSecretValue</c> by default.
        /// </para>
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerKnownSecrets(
            this IConfigurationBuilder builder,
            IEnumerable<string> secretIds,
            Action<SecretsManagerKnownSecretsOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            var ids = ValidateSecretIds(secretIds, nameof(secretIds));
            var options = new SecretsManagerKnownSecretsOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerKnownSecretsConfigurationSource(ids, options));
            return builder;
        }

        /// <summary>
        /// Adds a known-multiple-secrets AWS Secrets Manager configuration provider using the supplied <see cref="AWSOptions"/>.
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerKnownSecrets(
            this IConfigurationBuilder builder,
            AWSOptions awsOptions,
            IEnumerable<string> secretIds,
            Action<SecretsManagerKnownSecretsOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (awsOptions is null) throw new ArgumentNullException(nameof(awsOptions));
            var ids = ValidateSecretIds(secretIds, nameof(secretIds));
            var options = new SecretsManagerKnownSecretsOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerKnownSecretsConfigurationSource(awsOptions, ids, options));
            return builder;
        }

        /// <summary>
        /// Adds a known-multiple-secrets AWS Secrets Manager configuration provider using the supplied client.
        /// </summary>
        public static IConfigurationBuilder AddSecretsManagerKnownSecrets(
            this IConfigurationBuilder builder,
            IAmazonSecretsManager client,
            IEnumerable<string> secretIds,
            Action<SecretsManagerKnownSecretsOptions>? configure = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (client is null) throw new ArgumentNullException(nameof(client));
            var ids = ValidateSecretIds(secretIds, nameof(secretIds));
            var options = new SecretsManagerKnownSecretsOptions();
            configure?.Invoke(options);
            builder.Add(new SecretsManagerKnownSecretsConfigurationSource(client, ids, options));
            return builder;
        }

        #endregion

        private static void ValidateSecretId(string secretId, string paramName)
        {
            if (string.IsNullOrWhiteSpace(secretId))
                throw new ArgumentException("Secret id must not be null, empty, or whitespace.", paramName);
        }

        private static IReadOnlyList<string> ValidateSecretIds(IEnumerable<string>? secretIds, string paramName)
        {
            if (secretIds is null) throw new ArgumentNullException(paramName);
            var list = secretIds.ToList();
            if (list.Count == 0)
                throw new ArgumentException("At least one secret id must be provided.", paramName);
            for (int i = 0; i < list.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(list[i]))
                    throw new ArgumentException($"Secret id at index {i} must not be null, empty, or whitespace.", paramName);
            }
            return list;
        }
    }
}
