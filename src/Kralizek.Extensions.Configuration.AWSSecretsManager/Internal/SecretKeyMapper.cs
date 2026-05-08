using System;

using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal static class SecretKeyMapper
    {
        /// <summary>
        /// Validates the given <see cref="SecretKeyMappingOptions"/> and throws if any option is invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="SecretKeyMappingOptions.SecretNamePathSeparator"/> is an empty string.
        /// </exception>
        public static void ValidateOptions(SecretKeyMappingOptions options)
        {
            if (options.SecretNamePathSeparator is not null && options.SecretNamePathSeparator.Length == 0)
                throw new InvalidOperationException(
                    $"{nameof(SecretKeyMappingOptions.SecretNamePathSeparator)} cannot be an empty string. " +
                    "Set it to null to disable secret-name separator normalization.");
        }

        /// <summary>
        /// Produces the mapped configuration key for a JSON-derived secret entry.
        /// </summary>
        /// <param name="rawKey">
        /// The raw key as produced by <c>ExtractValues</c>, in the form <c>secretName:jsonPath</c>.
        /// </param>
        /// <param name="secretName">The secret name used as the prefix in <paramref name="rawKey"/>.</param>
        /// <param name="options">The key mapping options to apply.</param>
        /// <returns>The mapped configuration key.</returns>
        public static string MapJsonKey(string rawKey, string secretName, SecretKeyMappingOptions options)
        {
            // Extract the JSON-path portion from the raw key (everything after "secretName:").
            var prefix = $"{secretName}{ConfigurationPath.KeyDelimiter}";
            var jsonPath = rawKey.StartsWith(prefix, StringComparison.Ordinal)
                ? rawKey[prefix.Length..]
                : rawKey;

            string key;
            if (options.PrefixJsonKeysWithSecretName)
            {
                var mappedSecretName = ApplyPathSeparator(secretName, options.SecretNamePathSeparator);
                key = string.IsNullOrEmpty(mappedSecretName)
                    ? jsonPath
                    : ConfigurationPath.Combine(mappedSecretName, jsonPath);
            }
            else
            {
                key = jsonPath;
            }

            var targetSection = options.TargetSection;
            if (!string.IsNullOrEmpty(targetSection))
                key = ConfigurationPath.Combine(targetSection!, key);

            return key;
        }

        /// <summary>
        /// Produces the mapped configuration key for a scalar (non-JSON) secret.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="options">The key mapping options to apply.</param>
        /// <returns>The mapped configuration key.</returns>
        public static string MapScalarKey(string secretName, SecretKeyMappingOptions options)
        {
            var mappedSecretName = ApplyPathSeparator(secretName, options.SecretNamePathSeparator);
            var key = mappedSecretName;

            var targetSection = options.TargetSection;
            if (!string.IsNullOrEmpty(targetSection))
                key = ConfigurationPath.Combine(targetSection!, key);

            return key;
        }

        private static string ApplyPathSeparator(string secretName, string? separator)
        {
            if (separator is null) return secretName;
            return secretName
                .Replace(separator, ConfigurationPath.KeyDelimiter)
                .Trim(ConfigurationPath.KeyDelimiter[0]);
        }
    }
}