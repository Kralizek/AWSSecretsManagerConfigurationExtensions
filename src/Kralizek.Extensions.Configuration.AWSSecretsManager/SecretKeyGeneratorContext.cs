using System.Diagnostics.CodeAnalysis;

namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Provides contextual information used to generate the final configuration key
    /// for a value loaded from AWS Secrets Manager.
    /// </summary>
    /// <remarks>
    /// A context instance is created for each generated configuration key, not once per secret.
    /// JSON secrets can produce multiple contexts, one for each flattened JSON property.
    /// Scalar/simple secrets produce a single context.
    /// </remarks>
    public sealed class SecretKeyGeneratorContext
    {
        /// <summary>
        /// Gets the identifier used by the provider to request or resolve the secret.
        /// </summary>
        /// <remarks>
        /// For known-secret providers, this is the configured secret id.
        /// For discovery, this is the discovered secret identifier, usually the ARN when available.
        /// </remarks>
        public required string SecretId { get; init; }

        /// <summary>
        /// Gets the resolved AWS secret name used as the logical source name for key generation.
        /// </summary>
        public required string SecretName { get; init; }

        /// <summary>
        /// Gets the resolved AWS secret ARN, when available.
        /// </summary>
        public string? SecretArn { get; init; }

        /// <summary>
        /// Gets the key before built-in key mapping options are applied.
        /// </summary>
        public required string RawKey { get; init; }

        /// <summary>
        /// Gets the key after built-in key mapping options are applied.
        /// </summary>
        /// <remarks>
        /// The default key generator returns this value.
        /// </remarks>
        public required string DefaultKey { get; init; }

        /// <summary>
        /// Gets the JSON property path for this generated configuration key.
        /// </summary>
        /// <remarks>
        /// This value is non-null when this key is produced from a JSON property.
        /// It is null for scalar/simple secret values.
        /// </remarks>
        public string? JsonPath { get; init; }

        /// <summary>
        /// Gets a value indicating whether this generated configuration key was produced from a JSON property.
        /// </summary>
        /// <remarks>
        /// When this property is <see langword="true"/>, <see cref="JsonPath"/> is guaranteed to be non-null.
        /// </remarks>
        [MemberNotNullWhen(true, nameof(JsonPath))]
        public bool HasJsonPath => JsonPath is not null;
    }
}