using System;

using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal static class SecretKeyGeneratorContextFactory
    {
        public static SecretKeyGeneratorContext Create(
            string? secretId,
            string? secretName,
            string? secretArn,
            string rawKey,
            string defaultKey,
            string? rootKey = null)
        {
            var resolvedSecretId = !string.IsNullOrEmpty(secretId) ? secretId : secretName ?? string.Empty;
            var resolvedSecretName = !string.IsNullOrEmpty(secretName) ? secretName : resolvedSecretId;
            var resolvedRootKey = !string.IsNullOrEmpty(rootKey) ? rootKey : resolvedSecretName;

            return new SecretKeyGeneratorContext
            {
                SecretId = resolvedSecretId,
                SecretName = resolvedSecretName,
                SecretArn = secretArn,
                RawKey = rawKey,
                DefaultKey = defaultKey,
                JsonPath = GetJsonPath(resolvedRootKey, defaultKey)
            };
        }

        public static SecretKeyGeneratorContext CreateScalar(
            string? secretId,
            string? secretName,
            string? secretArn,
            string rawKey,
            string defaultKey)
        {
            var resolvedSecretId = !string.IsNullOrEmpty(secretId) ? secretId : secretName ?? string.Empty;
            var resolvedSecretName = !string.IsNullOrEmpty(secretName) ? secretName : resolvedSecretId;

            return new SecretKeyGeneratorContext
            {
                SecretId = resolvedSecretId,
                SecretName = resolvedSecretName,
                SecretArn = secretArn,
                RawKey = rawKey,
                DefaultKey = defaultKey,
                JsonPath = null
            };
        }

        private static string? GetJsonPath(string rootKey, string defaultKey)
        {
            var prefix = $"{rootKey}{ConfigurationPath.KeyDelimiter}";

            return defaultKey.StartsWith(prefix, StringComparison.Ordinal)
                ? defaultKey[prefix.Length..]
                : defaultKey;
        }
    }
}
