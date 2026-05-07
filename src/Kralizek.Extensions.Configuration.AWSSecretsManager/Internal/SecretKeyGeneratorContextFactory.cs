using System;

using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal static class SecretKeyGeneratorContextFactory
    {
        public static SecretKeyGeneratorContext Create(
            string secretId,
            string secretName,
            string? secretArn,
            string rawKey,
            string defaultKey)
        {
            return new SecretKeyGeneratorContext
            {
                SecretId = secretId,
                SecretName = secretName,
                SecretArn = secretArn,
                RawKey = rawKey,
                DefaultKey = defaultKey,
                JsonPath = GetJsonPath(secretName, defaultKey)
            };
        }

        public static SecretKeyGeneratorContext CreateScalar(
            string secretId,
            string secretName,
            string? secretArn,
            string rawKey,
            string defaultKey)
        {
            return new SecretKeyGeneratorContext
            {
                SecretId = secretId,
                SecretName = secretName,
                SecretArn = secretArn,
                RawKey = rawKey,
                DefaultKey = defaultKey,
                JsonPath = null
            };
        }

        private static string? GetJsonPath(string secretName, string defaultKey)
        {
            var prefix = $"{secretName}{ConfigurationPath.KeyDelimiter}";

            return defaultKey.StartsWith(prefix, StringComparison.Ordinal)
                ? defaultKey[prefix.Length..]
                : defaultKey;
        }
    }
}
