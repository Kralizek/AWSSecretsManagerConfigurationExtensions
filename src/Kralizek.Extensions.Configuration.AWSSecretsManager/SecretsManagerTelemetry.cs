namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Constants for integrating AWS Secrets Manager configuration providers with OpenTelemetry
    /// tracing and metrics.
    /// </summary>
    /// <remarks>
    /// To opt in, register the source and meter with your OpenTelemetry pipeline:
    /// <code>
    /// services.AddOpenTelemetry()
    ///     .WithTracing(b => b.AddSource(SecretsManagerTelemetry.ActivitySourceName))
    ///     .WithMetrics(b => b.AddMeter(SecretsManagerTelemetry.MeterName));
    /// </code>
    /// No additional configuration on the provider options is required.
    /// </remarks>
    public static class SecretsManagerTelemetry
    {
        /// <summary>
        /// The name of the <see cref="System.Diagnostics.ActivitySource"/> used to emit
        /// distributed-tracing spans from the Secrets Manager configuration providers.
        /// </summary>
        public const string ActivitySourceName = "Kralizek.Extensions.Configuration.AWSSecretsManager";

        /// <summary>
        /// The name of the <see cref="System.Diagnostics.Metrics.Meter"/> used to emit
        /// metrics from the Secrets Manager configuration providers.
        /// </summary>
        public const string MeterName = "Kralizek.Extensions.Configuration.AWSSecretsManager";
    }

    /// <summary>
    /// OpenTelemetry extension methods for <see cref="SecretsManagerDiscoveryOptions"/>.
    /// </summary>
    public static class SecretsManagerDiscoveryOptionsTelemetryExtensions
    {
        /// <summary>
        /// Opts this provider into OpenTelemetry instrumentation.
        /// No additional configuration is required beyond registering
        /// <see cref="SecretsManagerTelemetry.ActivitySourceName"/> and
        /// <see cref="SecretsManagerTelemetry.MeterName"/> with the OpenTelemetry SDK.
        /// </summary>
        public static SecretsManagerDiscoveryOptions UseOpenTelemetry(this SecretsManagerDiscoveryOptions options)
            => options;
    }

    /// <summary>
    /// OpenTelemetry extension methods for <see cref="SecretsManagerKnownSecretOptions"/>.
    /// </summary>
    public static class SecretsManagerKnownSecretOptionsTelemetryExtensions
    {
        /// <summary>
        /// Opts this provider into OpenTelemetry instrumentation.
        /// No additional configuration is required beyond registering
        /// <see cref="SecretsManagerTelemetry.ActivitySourceName"/> and
        /// <see cref="SecretsManagerTelemetry.MeterName"/> with the OpenTelemetry SDK.
        /// </summary>
        public static SecretsManagerKnownSecretOptions UseOpenTelemetry(this SecretsManagerKnownSecretOptions options)
            => options;
    }

    /// <summary>
    /// OpenTelemetry extension methods for <see cref="SecretsManagerKnownSecretsOptions"/>.
    /// </summary>
    public static class SecretsManagerKnownSecretsOptionsTelemetryExtensions
    {
        /// <summary>
        /// Opts this provider into OpenTelemetry instrumentation.
        /// No additional configuration is required beyond registering
        /// <see cref="SecretsManagerTelemetry.ActivitySourceName"/> and
        /// <see cref="SecretsManagerTelemetry.MeterName"/> with the OpenTelemetry SDK.
        /// </summary>
        public static SecretsManagerKnownSecretsOptions UseOpenTelemetry(this SecretsManagerKnownSecretsOptions options)
            => options;
    }
}
