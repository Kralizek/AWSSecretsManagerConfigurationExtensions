using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// Internal OpenTelemetry instrumentation singletons shared by all Secrets Manager
    /// configuration providers.
    /// </summary>
    internal static class SecretsManagerInstrumentation
    {
        internal static readonly ActivitySource ActivitySource =
            new ActivitySource(SecretsManagerTelemetry.ActivitySourceName);

        internal static readonly Meter Meter =
            new Meter(SecretsManagerTelemetry.MeterName);

        /// <summary>Time (ms) to complete the full initial secrets load.</summary>
        internal static readonly Histogram<double> LoadDuration =
            Meter.CreateHistogram<double>(
                "secretsmanager.load.duration",
                unit: "ms",
                description: "Time taken to complete the full initial secrets load.");

        /// <summary>Time (ms) to complete a secrets reload.</summary>
        internal static readonly Histogram<double> ReloadDuration =
            Meter.CreateHistogram<double>(
                "secretsmanager.reload.duration",
                unit: "ms",
                description: "Time taken to complete a secrets reload.");

        /// <summary>Number of failed secret reload attempts.</summary>
        internal static readonly Counter<long> ReloadErrors =
            Meter.CreateCounter<long>(
                "secretsmanager.reload.errors",
                description: "Number of failed secret reload attempts.");

        /// <summary>Number of secrets loaded per load or reload.</summary>
        internal static readonly Histogram<long> SecretsLoaded =
            Meter.CreateHistogram<long>(
                "secretsmanager.secrets.loaded",
                description: "Number of secrets loaded per load or reload.");
    }
}
