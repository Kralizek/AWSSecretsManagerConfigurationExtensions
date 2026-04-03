using System;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration
{
    public static class SecretsManagerLogging
    {
        public static Action<SecretsManagerLogEvent> ToLogEventSink(ILogger logger)
        {
            return evt =>
            {
                if (evt.Properties != null)
                {
                    using var scope = logger.BeginScope(evt.Properties);
                    logger.Log(evt.Level, evt.EventId, evt.Exception, evt.Message);
                }
                else
                {
                    logger.Log(evt.Level, evt.EventId, evt.Exception, evt.Message);
                }
            };
        }
    }

    public static class SecretsManagerOptionsLoggingExtensions
    {
        public static SecretsManagerOptions UseLogging(this SecretsManagerOptions options, ILogger logger)
        {
            options.LogEvent = SecretsManagerLogging.ToLogEventSink(logger);
            return options;
        }
    }

    public static class SecretsManagerBootstrapLogging
    {
        public static SecretsManagerOptions UseBootstrapLogging(
            this SecretsManagerOptions options,
            ILoggerFactory loggerFactory,
            string? categoryName = null)
        {
            var category = categoryName ?? "Kralizek.Extensions.Configuration.AWSSecretsManager";
            var logger = loggerFactory.CreateLogger(category);
            return options.UseLogging(logger);
        }
    }
}
