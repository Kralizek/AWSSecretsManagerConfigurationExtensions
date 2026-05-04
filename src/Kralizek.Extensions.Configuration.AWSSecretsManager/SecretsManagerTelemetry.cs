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
}