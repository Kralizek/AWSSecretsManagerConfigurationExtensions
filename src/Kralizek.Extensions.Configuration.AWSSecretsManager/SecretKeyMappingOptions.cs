namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Controls how AWS secret names and values are projected into .NET configuration keys.
    /// </summary>
    /// <remarks>
    /// These options are shared by all provider modes: Discovery, KnownSecret, and KnownSecrets.
    /// The resulting mapped key is passed as <see cref="SecretKeyGeneratorContext.DefaultKey"/> to
    /// the <c>KeyGenerator</c> delegate, where it can be further customized.
    /// </remarks>
    public sealed class SecretKeyMappingOptions
    {
        /// <summary>
        /// Gets or sets the separator used to split the secret-name portion of generated keys
        /// into .NET configuration path segments.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set, occurrences of this value in the secret-name portion are replaced with
        /// <see cref="Microsoft.Extensions.Configuration.ConfigurationPath.KeyDelimiter"/> (<c>:</c>).
        /// Leading and trailing delimiters produced by path-style secret names are trimmed.
        /// </para>
        /// <para>
        /// Set to <see langword="null"/> to disable secret-name separator normalization.
        /// An empty string is invalid and will cause an <see cref="System.InvalidOperationException"/>
        /// to be thrown when secrets are loaded.
        /// </para>
        /// <para>Examples:</para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <c>"/"</c> (the default) maps <c>/my-app/production/database</c>
        ///       to <c>my-app:production:database</c>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>"--"</c> maps <c>my-app--production--database</c>
        ///       to <c>my-app:production:database</c>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <see langword="null"/> disables normalization; secret names are used as-is.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string? SecretNamePathSeparator { get; set; } = "/";

        /// <summary>
        /// Gets or sets a value indicating whether JSON-derived configuration keys include
        /// the mapped secret name as a prefix.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <see langword="true"/> (the default), JSON-derived keys are namespaced under the
        /// mapped secret name, reducing the risk of key collisions across secrets.
        /// </para>
        /// <para>
        /// When <see langword="false"/>, the secret name is stripped and only the JSON property path
        /// is used as the configuration key. This is useful when loading a JSON secret directly as
        /// normal application configuration, or when projecting it into a specific
        /// <see cref="TargetSection"/>.
        /// </para>
        /// <para>
        /// This option has no effect on scalar/simple secrets; their key is always derived from
        /// the mapped secret name.
        /// </para>
        /// </remarks>
        public bool PrefixJsonKeysWithSecretName { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional configuration section prepended to all generated keys.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set, all generated keys — both JSON-derived and scalar — are placed under this section.
        /// </para>
        /// <para>
        /// Example: with <c>TargetSection = "Email"</c>, a JSON key <c>Smtp:Host</c>
        /// becomes <c>Email:Smtp:Host</c>.
        /// </para>
        /// </remarks>
        public string? TargetSection { get; set; }
    }
}