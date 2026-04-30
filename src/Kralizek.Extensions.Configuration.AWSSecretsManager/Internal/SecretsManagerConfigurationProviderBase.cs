using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// Abstract base class that provides the shared load/reload/poll/dispose infrastructure
    /// for all AWS Secrets Manager configuration providers.
    /// Subclasses implement <see cref="FetchConfigurationCoreAsync"/> to supply secrets.
    /// </summary>
    public abstract class SecretsManagerConfigurationProviderBase : ConfigurationProvider, IDisposable
    {
        private Dictionary<string, string?> _loadedValues = new(StringComparer.InvariantCultureIgnoreCase);
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>Gets the optional reload interval for this provider.</summary>
        protected abstract TimeSpan? ReloadInterval { get; }

        /// <summary>Gets the log event sink for this provider, or <see langword="null"/> if logging is disabled.</summary>
        protected abstract Action<SecretsManagerLogEvent>? LogEventSink { get; }

        /// <summary>Gets the duplicate key handling policy for this provider.</summary>
        protected abstract DuplicateKeyHandling DuplicateKeyHandling { get; }

        /// <summary>
        /// Gets a short string that identifies the provider type used as the <c>provider.type</c>
        /// tag on OpenTelemetry spans and metrics (e.g. <c>"Discovery"</c>, <c>"KnownSecret"</c>,
        /// <c>"KnownSecrets"</c>).
        /// </summary>
        protected abstract string ProviderType { get; }

        /// <summary>
        /// Fetches the complete configuration dictionary from AWS.
        /// Called on initial load and on every reload.
        /// </summary>
        protected abstract Task<Dictionary<string, string?>> FetchConfigurationCoreAsync(CancellationToken cancellationToken);

        /// <summary>Sends a structured log event to the configured sink, if any.</summary>
        protected void Log(LogLevel level, EventId eventId, string message, Exception? ex = null, params object?[] args)
            => LogEventSink?.Invoke(new SecretsManagerLogEvent(level, eventId, message, ex, Args: args));

        /// <summary>
        /// Applies a single key/value pair to the accumulating dictionary,
        /// respecting the <see cref="DuplicateKeyHandling"/> policy.
        /// </summary>
        protected void ApplyEntry(Dictionary<string, string?> dict, string key, string value)
        {
            switch (DuplicateKeyHandling)
            {
                case DuplicateKeyHandling.Throw:
                    if (dict.ContainsKey(key))
                        throw new InvalidOperationException(
                            $"Duplicate configuration key '{key}' found in AWS Secrets Manager. " +
                            "Set DuplicateKeyHandling to FirstWins or LastWins to suppress this error.");
                    dict[key] = value;
                    break;
                case DuplicateKeyHandling.FirstWins:
                    if (!dict.ContainsKey(key))
                        dict[key] = value;
                    else
                        Log(LogLevel.Debug, SecretsManagerLogEvents.DuplicateKeyResolved,
                            "Duplicate configuration key {Key} resolved (FirstWins); existing value kept.", args: key);
                    break;
                case DuplicateKeyHandling.LastWins:
                default:
                    if (dict.ContainsKey(key))
                        Log(LogLevel.Debug, SecretsManagerLogEvents.DuplicateKeyResolved,
                            "Duplicate configuration key {Key} resolved (LastWins); value overwritten.", args: key);
                    dict[key] = value;
                    break;
            }
        }

        /// <inheritdoc/>
        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>Forces an immediate reload outside of the normal polling schedule.</summary>
        public Task ForceReloadAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

        private async Task LoadAsync()
        {
            Log(LogLevel.Debug, SecretsManagerLogEvents.LoadStarted, "Secrets Manager configuration load started.");

            using var activity = SecretsManagerInstrumentation.ActivitySource.StartActivity("secretsmanager load");
            activity?.SetTag("provider.type", ProviderType);

            var startTimestamp = Stopwatch.GetTimestamp();
            try
            {
                _loadedValues = await FetchConfigurationCoreAsync(default).ConfigureAwait(false);
                SetData(_loadedValues, triggerReload: false);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                var elapsedMs = GetElapsedMilliseconds(startTimestamp);
                SecretsManagerInstrumentation.LoadDuration.Record(
                    elapsedMs,
                    new KeyValuePair<string, object?>("provider.type", ProviderType));
            }

            activity?.SetTag("secret.count", _loadedValues.Count);
            SecretsManagerInstrumentation.SecretsLoaded.Record(
                _loadedValues.Count,
                new KeyValuePair<string, object?>("provider.type", ProviderType));

            Log(LogLevel.Debug, SecretsManagerLogEvents.LoadCompleted,
                "Secrets Manager configuration load completed. {SecretCount} secrets loaded.",
                args: _loadedValues.Count);

            if (ReloadInterval.HasValue)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _pollingTask = PollForChangesAsync(ReloadInterval.Value, _cancellationTokenSource.Token);
            }
        }

        private async Task PollForChangesAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try { await Task.Delay(interval, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                try { await ReloadAsync(cancellationToken).ConfigureAwait(false); }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    SecretsManagerInstrumentation.ReloadErrors.Add(
                        1,
                        new KeyValuePair<string, object?>("provider.type", ProviderType));
                    Log(LogLevel.Error, SecretsManagerLogEvents.ReloadFailed,
                        "Secrets Manager configuration reload failed.", ex);
                }
            }
        }

        private async Task ReloadAsync(CancellationToken cancellationToken)
        {
            Log(LogLevel.Debug, SecretsManagerLogEvents.ReloadStarted, "Secrets Manager configuration reload started.");

            using var activity = SecretsManagerInstrumentation.ActivitySource.StartActivity("secretsmanager reload");
            activity?.SetTag("provider.type", ProviderType);

            var startTimestamp = Stopwatch.GetTimestamp();
            bool changed = false;
            try
            {
                var oldValues = _loadedValues;
                var newValues = await FetchConfigurationCoreAsync(cancellationToken).ConfigureAwait(false);

                changed = !SecretsManagerHelpers.DictionaryEquals(oldValues, newValues);
                if (changed)
                {
                    _loadedValues = newValues;
                    SetData(_loadedValues, triggerReload: true);
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
            finally
            {
                var elapsedMs = GetElapsedMilliseconds(startTimestamp);
                SecretsManagerInstrumentation.ReloadDuration.Record(
                    elapsedMs,
                    new KeyValuePair<string, object?>("provider.type", ProviderType),
                    new KeyValuePair<string, object?>("changed", changed));
            }

            activity?.SetTag("changed", changed);
            Log(LogLevel.Debug, SecretsManagerLogEvents.ReloadCompleted, "Secrets Manager configuration reload completed.");
        }

        private static double GetElapsedMilliseconds(long startTimestamp)
            => (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

        private void SetData(Dictionary<string, string?> values, bool triggerReload)
        {
            Data = values;
            if (triggerReload) OnReload();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            var cancellationTokenSource = _cancellationTokenSource;
            var pollingTask = _pollingTask;
            _cancellationTokenSource = null;
            _pollingTask = null;
            cancellationTokenSource?.Cancel();
            try { pollingTask?.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
            finally { cancellationTokenSource?.Dispose(); }
        }
    }
}