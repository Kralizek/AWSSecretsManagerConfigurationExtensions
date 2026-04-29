using System;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration
{
    /// <summary>
    /// Helpers for bridging <see cref="SecretsManagerLogEvent"/> to <see cref="ILogger"/>.
    /// </summary>
    public static class SecretsManagerLogging
    {
        /// <summary>
        /// Creates an <see cref="Action{SecretsManagerLogEvent}"/> sink that forwards log events to an <see cref="ILogger"/>.
        /// </summary>
        public static Action<SecretsManagerLogEvent> ToLogEventSink(ILogger logger)
        {
            return evt =>
            {
                using var scope = evt.Properties != null ? logger.BeginScope(evt.Properties) : null;
                logger.Log(evt.Level, evt.EventId, evt.Exception, evt.Message, evt.Args ?? []);
            };
        }
    }

    /// <summary>
    /// Logging extension methods for <see cref="SecretsManagerDiscoveryOptions"/>.
    /// </summary>
    public static class SecretsManagerDiscoveryOptionsLoggingExtensions
    {
        /// <summary>
        /// Configures the discovery options to forward log events to the supplied <see cref="ILogger"/>.
        /// </summary>
        public static SecretsManagerDiscoveryOptions UseLogging(this SecretsManagerDiscoveryOptions options, ILogger logger)
        {
            options.LogEvent = SecretsManagerLogging.ToLogEventSink(logger);
            return options;
        }

        /// <summary>
        /// Configures the discovery options to forward log events using a logger created from
        /// the supplied <see cref="ILoggerFactory"/>.
        /// </summary>
        public static SecretsManagerDiscoveryOptions UseBootstrapLogging(
            this SecretsManagerDiscoveryOptions options,
            ILoggerFactory loggerFactory,
            string? categoryName = null)
        {
            var category = categoryName ?? "Kralizek.Extensions.Configuration.AWSSecretsManager";
            var logger = loggerFactory.CreateLogger(category);
            return options.UseLogging(logger);
        }
    }

    /// <summary>
    /// Logging extension methods for <see cref="SecretsManagerKnownSecretOptions"/>.
    /// </summary>
    public static class SecretsManagerKnownSecretOptionsLoggingExtensions
    {
        /// <summary>
        /// Configures the known-secret options to forward log events to the supplied <see cref="ILogger"/>.
        /// </summary>
        public static SecretsManagerKnownSecretOptions UseLogging(this SecretsManagerKnownSecretOptions options, ILogger logger)
        {
            options.LogEvent = SecretsManagerLogging.ToLogEventSink(logger);
            return options;
        }

        /// <summary>
        /// Configures the known-secret options to forward log events using a logger created from
        /// the supplied <see cref="ILoggerFactory"/>.
        /// </summary>
        public static SecretsManagerKnownSecretOptions UseBootstrapLogging(
            this SecretsManagerKnownSecretOptions options,
            ILoggerFactory loggerFactory,
            string? categoryName = null)
        {
            var category = categoryName ?? "Kralizek.Extensions.Configuration.AWSSecretsManager";
            var logger = loggerFactory.CreateLogger(category);
            return options.UseLogging(logger);
        }
    }

    /// <summary>
    /// Logging extension methods for <see cref="SecretsManagerKnownSecretsOptions"/>.
    /// </summary>
    public static class SecretsManagerKnownSecretsOptionsLoggingExtensions
    {
        /// <summary>
        /// Configures the known-secrets options to forward log events to the supplied <see cref="ILogger"/>.
        /// </summary>
        public static SecretsManagerKnownSecretsOptions UseLogging(this SecretsManagerKnownSecretsOptions options, ILogger logger)
        {
            options.LogEvent = SecretsManagerLogging.ToLogEventSink(logger);
            return options;
        }

        /// <summary>
        /// Configures the known-secrets options to forward log events using a logger created from
        /// the supplied <see cref="ILoggerFactory"/>.
        /// </summary>
        public static SecretsManagerKnownSecretsOptions UseBootstrapLogging(
            this SecretsManagerKnownSecretsOptions options,
            ILoggerFactory loggerFactory,
            string? categoryName = null)
        {
            var category = categoryName ?? "Kralizek.Extensions.Configuration.AWSSecretsManager";
            var logger = loggerFactory.CreateLogger(category);
            return options.UseLogging(logger);
        }
    }
}

