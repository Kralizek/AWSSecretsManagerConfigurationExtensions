using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using AutoFixture;
using AutoFixture.NUnit3;

using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;

using Moq;

using NUnit.Framework;

using Tests.Types;

namespace Tests.Internal
{
    [TestFixture]
    public class SecretsManagerTelemetryTests
    {
        // ── ActivitySource / span tests ──────────────────────────────────────

        [Test, CustomAutoData]
        public void Load_emits_load_span(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecretsManagerTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == "secretsmanager load")
                        captured = activity;
                }
            };
            ActivitySource.AddActivityListener(listener);

            sut.Load();

            Assert.That(captured, Is.Not.Null, "Expected a 'secretsmanager load' span.");
            Assert.That(captured!.GetTagItem("provider.type"), Is.EqualTo("Discovery"));
            Assert.That(captured.GetTagItem("secret.count"), Is.Not.Null);
        }

        [Test, CustomAutoData]
        public void Load_emits_ListSecrets_child_span(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            var capturedNames = new List<string>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecretsManagerTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => capturedNames.Add(activity.OperationName)
            };
            ActivitySource.AddActivityListener(listener);

            sut.Load();

            Assert.That(capturedNames, Has.Member("secretsmanager ListSecrets"));
        }

        [Test, CustomAutoData]
        public void Load_emits_GetSecretValue_child_span(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecretsManagerTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == "secretsmanager GetSecretValue")
                        captured = activity;
                }
            };
            ActivitySource.AddActivityListener(listener);

            sut.Load();

            Assert.That(captured, Is.Not.Null, "Expected a 'secretsmanager GetSecretValue' span.");
            Assert.That(captured!.GetTagItem("aws.secretsmanager.secret.name"), Is.Not.Null);
            Assert.That(captured.GetTagItem("aws.secretsmanager.secret.arn"), Is.Not.Null);
        }

        [Test, CustomAutoData]
        public void Batch_load_emits_BatchGetSecretValue_child_span(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut,
            IFixture fixture)
        {
            options.UseBatchFetch = true;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            var batchResp = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry>
                {
                    new SecretValueEntry { ARN = testEntry.ARN, Name = testEntry.Name, SecretString = "val" }
                })
                .Without(p => p.Errors)
                .Without(p => p.NextToken)
                .Create();
            Mock.Get(secretsManager)
                .Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(batchResp);

            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecretsManagerTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == "secretsmanager BatchGetSecretValue")
                        captured = activity;
                }
            };
            ActivitySource.AddActivityListener(listener);

            sut.Load();

            Assert.That(captured, Is.Not.Null, "Expected a 'secretsmanager BatchGetSecretValue' span.");
            Assert.That(captured!.GetTagItem("batch.size"), Is.Not.Null);
            Assert.That(captured.GetTagItem("page.count"), Is.Not.Null);
        }

        [Test, CustomAutoData]
        public void KnownSecret_load_emits_load_span_with_KnownSecret_provider_type(
            [Frozen] IAmazonSecretsManager secretsManager,
            GetSecretValueResponse getSecretValueResponse)
        {
            const string secretId = "my-telemetry-secret";
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecretsManagerTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == "secretsmanager load")
                        captured = activity;
                }
            };
            ActivitySource.AddActivityListener(listener);

            sut.Load();

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.GetTagItem("provider.type"), Is.EqualTo("KnownSecret"));
        }

        [Test, CustomAutoData]
        public void KnownSecrets_load_emits_load_span_with_KnownSecrets_provider_type(
            [Frozen] IAmazonSecretsManager secretsManager,
            GetSecretValueResponse getSecretValueResponse)
        {
            var sut = new SecretsManagerKnownSecretsConfigurationProvider(
                secretsManager,
                new[] { "secret-a" },
                new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == SecretsManagerTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (activity.OperationName == "secretsmanager load")
                        captured = activity;
                }
            };
            ActivitySource.AddActivityListener(listener);

            sut.Load();

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.GetTagItem("provider.type"), Is.EqualTo("KnownSecrets"));
        }

        // ── Metrics tests ────────────────────────────────────────────────────

        [Test, CustomAutoData]
        public void Load_records_load_duration_metric(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            double? recorded = null;
            using var meterListener = new MeterListener();
            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SecretsManagerTelemetry.MeterName &&
                    instrument.Name == "secretsmanager.load.duration")
                    listener.EnableMeasurementEvents(instrument);
            };
            meterListener.SetMeasurementEventCallback<double>((_, value, _, _) => recorded = value);
            meterListener.Start();

            sut.Load();

            Assert.That(recorded, Is.Not.Null, "Expected secretsmanager.load.duration to be recorded.");
            Assert.That(recorded, Is.GreaterThanOrEqualTo(0));
        }

        [Test, CustomAutoData]
        public void Load_records_secrets_loaded_metric(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueResponse);

            long? recorded = null;
            using var meterListener = new MeterListener();
            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SecretsManagerTelemetry.MeterName &&
                    instrument.Name == "secretsmanager.secrets.loaded")
                    listener.EnableMeasurementEvents(instrument);
            };
            meterListener.SetMeasurementEventCallback<long>((_, value, _, _) => recorded = value);
            meterListener.Start();

            sut.Load();

            Assert.That(recorded, Is.Not.Null, "Expected secretsmanager.secrets.loaded to be recorded.");
            Assert.That(recorded, Is.GreaterThanOrEqualTo(0));
        }

        [Test, CustomAutoData]
        public async System.Threading.Tasks.Task Reload_records_reload_duration_metric(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse initial,
            GetSecretValueResponse updated,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager)
                .SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial)
                .ReturnsAsync(updated);

            sut.Load();

            double? recorded = null;
            using var meterListener = new MeterListener();
            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == SecretsManagerTelemetry.MeterName &&
                    instrument.Name == "secretsmanager.reload.duration")
                    listener.EnableMeasurementEvents(instrument);
            };
            meterListener.SetMeasurementEventCallback<double>((_, value, _, _) => recorded = value);
            meterListener.Start();

            await sut.ForceReloadAsync(CancellationToken.None);

            Assert.That(recorded, Is.Not.Null, "Expected secretsmanager.reload.duration to be recorded.");
            Assert.That(recorded, Is.GreaterThanOrEqualTo(0));
        }

        // ── SecretsManagerTelemetry public API tests ─────────────────────────

        [Test]
        public void ActivitySourceName_constant_is_correct()
        {
            Assert.That(SecretsManagerTelemetry.ActivitySourceName,
                Is.EqualTo("Kralizek.Extensions.Configuration.AWSSecretsManager"));
        }

        [Test]
        public void MeterName_constant_is_correct()
        {
            Assert.That(SecretsManagerTelemetry.MeterName,
                Is.EqualTo("Kralizek.Extensions.Configuration.AWSSecretsManager"));
        }
    }
}