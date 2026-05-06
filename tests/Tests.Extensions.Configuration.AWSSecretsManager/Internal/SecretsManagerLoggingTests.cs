// #86 – Logging coverage: tests that verify structured log events are emitted for all
// diagnostically important situations, and that no secret value ever appears in a log message.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using AutoFixture;
using AutoFixture.NUnit3;

using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace Tests.Internal
{
    [TestFixture]
    [Description("#86: Diagnostic logging coverage – verifies log events are emitted for important provider situations.")]
    public class SecretsManagerLoggingTests
    {
        // ── Helpers ─────────────────────────────────────────────────────────────

        private static List<SecretsManagerLogEvent> CaptureEvents(Action<SecretsManagerDiscoveryOptions> configure)
        {
            var events = new List<SecretsManagerLogEvent>();
            configure(new SecretsManagerDiscoveryOptions { LogEvent = e => events.Add(e) });
            return events;
        }

        // ── Discovery provider – SecretLoaded ───────────────────────────────────

        [Test, CustomAutoData]
        [Description("#86: SecretLoaded event is emitted (with secret name, not value) when a secret is successfully loaded via GetSecretValue.")]
        public void SecretLoaded_event_is_emitted_when_discovery_loads_a_secret(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string secretName = "my-app-secret";
            const string secretValue = "super-secret-value";

            var listResponse = new ListSecretsResponse
            {
                SecretList = new List<SecretListEntry>
                {
                    new SecretListEntry { ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-app-secret-AbCdEf", Name = secretName }
                }
            };
            var getResponse = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, secretValue)
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getResponse);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.SecretLoaded),
                "Expected at least one SecretLoaded log event.");
        }

        [Test, CustomAutoData]
        [Description("#86: The SecretLoaded log event must contain the secret name but not the secret value.")]
        public void SecretLoaded_log_event_does_not_contain_secret_value(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string secretName = "my-app-secret";
            const string secretValue = "HIGHLY_SENSITIVE_VALUE_XYZ";

            var listResponse = new ListSecretsResponse
            {
                SecretList = new List<SecretListEntry>
                {
                    new SecretListEntry { ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-app-secret-AbCdEf", Name = secretName }
                }
            };
            var getResponse = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, secretValue)
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getResponse);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            foreach (var evt in loggedEvents)
            {
                // Neither the raw message template nor the formatted args must carry the secret value.
                Assert.That(evt.Message, Does.Not.Contain(secretValue),
                    $"Log message template must not contain the secret value. EventId={evt.EventId.Name}");

                if (evt.Args != null)
                {
                    foreach (var arg in evt.Args)
                    {
                        Assert.That(arg?.ToString(), Does.Not.EqualTo(secretValue),
                            $"Log args must not contain the secret value. EventId={evt.EventId.Name}");
                    }
                }
            }
        }

        // ── Discovery provider – SecretSkipped ──────────────────────────────────

        [Test, CustomAutoData]
        [Description("#86: SecretSkipped event is emitted when a secret is excluded by SecretFilter (per-secret GetSecretValue path).")]
        public void SecretSkipped_event_is_emitted_when_SecretFilter_excludes_a_secret(
            [Frozen] IAmazonSecretsManager secretsManager)
        {
            var listResponse = new ListSecretsResponse
            {
                SecretList = new List<SecretListEntry>
                {
                    new SecretListEntry { ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf", Name = "my-secret" }
                }
            };

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(listResponse);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                SecretFilter = _ => false,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.SecretSkipped),
                "Expected a SecretSkipped log event when SecretFilter returns false.");
        }

        // ── Discovery provider – DuplicateKeyResolved ───────────────────────────

        [Test, CustomAutoData]
        [Description("#86: DuplicateKeyResolved event is emitted when FirstWins keeps the existing value.")]
        public void DuplicateKeyResolved_event_is_emitted_with_FirstWins(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string duplicateKey = "SHARED_KEY";
            var s1 = new SecretListEntry { ARN = "arn1", Name = "s1" };
            var s2 = new SecretListEntry { ARN = "arn2", Name = "s2" };

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { s1, s2 } });
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn1"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "first").Without(p => p.SecretBinary).Create());
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn2"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "second").Without(p => p.SecretBinary).Create());

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                DuplicateKeyHandling = DuplicateKeyHandling.FirstWins,
                KeyGenerator = (_, _) => duplicateKey,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(sut.Get(duplicateKey), Is.EqualTo("first"),
                "FirstWins: the first value must be kept.");
            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.DuplicateKeyResolved),
                "Expected a DuplicateKeyResolved log event for FirstWins.");
        }

        [Test, CustomAutoData]
        [Description("#86: DuplicateKeyResolved event is emitted when LastWins overwrites the earlier value.")]
        public void DuplicateKeyResolved_event_is_emitted_with_LastWins(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string duplicateKey = "SHARED_KEY";
            var s1 = new SecretListEntry { ARN = "arn1", Name = "s1" };
            var s2 = new SecretListEntry { ARN = "arn2", Name = "s2" };

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { s1, s2 } });
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn1"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "first").Without(p => p.SecretBinary).Create());
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn2"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "last").Without(p => p.SecretBinary).Create());

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                DuplicateKeyHandling = DuplicateKeyHandling.LastWins,
                KeyGenerator = (_, _) => duplicateKey,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(sut.Get(duplicateKey), Is.EqualTo("last"),
                "LastWins: the last value must overwrite the first.");
            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.DuplicateKeyResolved),
                "Expected a DuplicateKeyResolved log event for LastWins.");
        }

        [Test, CustomAutoData]
        [Description("#86: The DuplicateKeyResolved log event arg must contain the key name, never the duplicate values.")]
        public void DuplicateKeyResolved_log_event_contains_key_name_not_values(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string duplicateKey = "SHARED_KEY";
            const string sensitiveValue = "SENSITIVE_VALUE_NOT_IN_LOGS";
            var s1 = new SecretListEntry { ARN = "arn1", Name = "s1" };
            var s2 = new SecretListEntry { ARN = "arn2", Name = "s2" };

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { s1, s2 } });
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn1"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, sensitiveValue).Without(p => p.SecretBinary).Create());
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn2"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, sensitiveValue + "_2").Without(p => p.SecretBinary).Create());

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                DuplicateKeyHandling = DuplicateKeyHandling.FirstWins,
                KeyGenerator = (_, _) => duplicateKey,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            var dupeEvent = loggedEvents.Find(e => e.EventId == SecretsManagerLogEvents.DuplicateKeyResolved);
            Assert.That(dupeEvent, Is.Not.Null);

            // The key name must appear in the args so the operator can diagnose the collision.
            var argsStr = string.Join(" ", dupeEvent!.Args ?? Array.Empty<object?>());
            Assert.That(argsStr, Does.Contain(duplicateKey), "DuplicateKeyResolved arg must identify the key.");

            // Neither sensitive value must appear anywhere in the event.
            Assert.That(dupeEvent.Message, Does.Not.Contain(sensitiveValue));
            Assert.That(argsStr, Does.Not.Contain(sensitiveValue));
        }

        // ── KnownSecret provider – SecretLoaded ─────────────────────────────────

        [Test, CustomAutoData]
        [Description("#86: SecretLoaded event is emitted when the KnownSecret provider successfully loads.")]
        public void KnownSecret_SecretLoaded_event_is_emitted(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string secretName = "my-known-secret";
            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, "value")
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerKnownSecretOptions
            {
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretName, options);
            sut.Load();

            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.SecretLoaded),
                "Expected a SecretLoaded log event.");
        }

        [Test, CustomAutoData]
        [Description("#86: The KnownSecret SecretLoaded event must not contain the secret value.")]
        public void KnownSecret_SecretLoaded_event_does_not_contain_secret_value(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string secretName = "my-known-secret";
            const string secretValue = "SENSITIVE_SECRET_VALUE_MUST_NOT_APPEAR_IN_LOGS";

            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, secretValue)
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerKnownSecretOptions
            {
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretName, options);
            sut.Load();

            foreach (var evt in loggedEvents)
            {
                Assert.That(evt.Message, Does.Not.Contain(secretValue),
                    $"Log message template must not contain the secret value. EventId={evt.EventId.Name}");
                if (evt.Args != null)
                {
                    foreach (var arg in evt.Args)
                    {
                        Assert.That(arg?.ToString(), Does.Not.EqualTo(secretValue),
                            $"Log args must not contain the secret value. EventId={evt.EventId.Name}");
                    }
                }
            }
        }

        // ── Discovery provider – ReloadFailed ───────────────────────────────────

        [Test, CustomAutoData]
        [Description("#86: ReloadFailed event is emitted (with the exception) when a background poll-reload throws.")]
        public void ReloadFailed_event_is_emitted_when_a_background_reload_throws(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            const string secretName = "my-secret";
            var initial = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, "initial")
                .Without(p => p.SecretBinary)
                .Create();

            var reloadFailed = new ManualResetEventSlim();

            Mock.Get(secretsManager)
                .SetupSequence(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse
                {
                    SecretList = new List<SecretListEntry>
                    {
                        new SecretListEntry { ARN = "arn1", Name = secretName }
                    }
                })
                .ThrowsAsync(new Exception("Simulated AWS error during reload"));

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                ReloadInterval = TimeSpan.FromMilliseconds(50),
                LogEvent = e =>
                {
                    loggedEvents.Add(e);
                    if (e.EventId == SecretsManagerLogEvents.ReloadFailed)
                        reloadFailed.Set();
                }
            };

            using var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(reloadFailed.Wait(TimeSpan.FromSeconds(5)), Is.True,
                "Expected a ReloadFailed log event within 5 seconds.");

            var failedEvent = loggedEvents.Find(e => e.EventId == SecretsManagerLogEvents.ReloadFailed);
            Assert.That(failedEvent, Is.Not.Null);
            Assert.That(failedEvent!.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(failedEvent.Exception, Is.Not.Null, "ReloadFailed event should carry the exception.");
        }

        // ── LoadStarted / LoadCompleted lifecycle events ─────────────────────────

        [Test, CustomAutoData]
        [Description("#86: LoadStarted and LoadCompleted events are emitted around a successful load.")]
        public void Load_emits_LoadStarted_and_LoadCompleted_lifecycle_events(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            var getResponse = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, "my-secret")
                .With(p => p.SecretString, "v")
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse
                {
                    SecretList = new List<SecretListEntry>
                    {
                        new SecretListEntry { ARN = "arn1", Name = "my-secret" }
                    }
                });
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getResponse);

            var loggedEvents = new List<SecretsManagerLogEvent>();
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                LogEvent = e => loggedEvents.Add(e)
            };

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.LoadStarted),
                "Expected a LoadStarted log event.");
            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.LoadCompleted),
                "Expected a LoadCompleted log event.");
        }
    }
}