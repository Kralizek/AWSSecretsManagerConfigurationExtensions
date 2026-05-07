using System.Diagnostics.CodeAnalysis;

namespace Kralizek.Extensions.Configuration
{
    public sealed class SecretKeyGeneratorContext
    {
        public required string SecretId { get; init; }

        public required string SecretName { get; init; }

        public string? SecretArn { get; init; }

        /// <summary>
        /// The key before built-in key mapping options are applied.
        /// </summary>
        public required string RawKey { get; init; }

        /// <summary>
        /// The key after built-in key mapping options are applied.
        /// The default key generator returns this value.
        /// </summary>
        public required string DefaultKey { get; init; }

        /// <summary>
        /// The JSON property path for this generated configuration key.
        /// Non-null when this key comes from a JSON property; null for scalar/simple secret values.
        /// </summary>
        public string? JsonPath { get; init; }

        [MemberNotNullWhen(true, nameof(JsonPath))]
        public bool HasJsonPath => JsonPath is not null;
    }
}
