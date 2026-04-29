using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    [TestOf(typeof(SecretsManagerKnownSecretsConfigurationProvider))]
    public class SecretsManagerKnownSecretsConfigurationProviderTests
    {
        [Test, CustomAutoData]
        public void Never_calls_ListSecrets([Frozen] IAmazonSecretsManager secretsManager, GetSecretValueResponse response, IFixture fixture)
        {
            response.SecretString = "val";
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1" }, new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });
            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test, CustomAutoData]
        public void Uses_BatchGetSecretValue_by_default([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var batchResponse = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry>
                {
                    new SecretValueEntry { ARN = "s1", Name = "s1", SecretString = "val1" }
                })
                .With(p => p.Errors, new List<APIErrorType>())
                .Without(p => p.NextToken)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(batchResponse);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1" }, new SecretsManagerKnownSecretsOptions());
            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test, CustomAutoData]
        public void Uses_GetSecretValue_per_id_when_batch_disabled([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var r1 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s1").With(p => p.SecretString, "v1").Without(p => p.SecretBinary).Create();
            var r2 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s2").With(p => p.SecretString, "v2").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s1"), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s2"), It.IsAny<CancellationToken>())).ReturnsAsync(r2);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1", "s2" }, new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });
            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            Mock.Get(secretsManager).Verify(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test, CustomAutoData]
        public void Batch_chunks_at_20_secrets([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var secretIds = Enumerable.Range(1, 25).Select(i => $"secret-{i}").ToList();

            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BatchGetSecretValueRequest req, CancellationToken _) =>
                {
                    var values = req.SecretIdList.Select(id => new SecretValueEntry { ARN = id, Name = id, SecretString = "val" }).ToList();
                    return fixture.Build<BatchGetSecretValueResponse>()
                        .With(p => p.SecretValues, values)
                        .With(p => p.Errors, new List<APIErrorType>())
                        .Without(p => p.NextToken)
                        .Create();
                });

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, secretIds, new SecretsManagerKnownSecretsOptions());
            sut.Load();

            // 25 secrets split across 2 chunks (20 + 5)
            Mock.Get(secretsManager).Verify(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test, CustomAutoData]
        public void Simple_string_values_loaded_per_id_with_GetSecretValue([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var r1 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s1").With(p => p.SecretString, "value1").Without(p => p.SecretBinary).Create();
            var r2 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s2").With(p => p.SecretString, "value2").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s1"), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s2"), It.IsAny<CancellationToken>())).ReturnsAsync(r2);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1", "s2" }, new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });
            sut.Load();

            Assert.That(sut.Get("s1"), Is.EqualTo("value1"));
            Assert.That(sut.Get("s2"), Is.EqualTo("value2"));
        }

        [Test, CustomAutoData]
        public void Batch_values_loaded_by_name([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var batchResponse = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry>
                {
                    new SecretValueEntry { ARN = "arn:s1", Name = "s1", SecretString = "value1" },
                    new SecretValueEntry { ARN = "arn:s2", Name = "s2", SecretString = "value2" }
                })
                .With(p => p.Errors, new List<APIErrorType>())
                .Without(p => p.NextToken)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(batchResponse);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1", "s2" }, new SecretsManagerKnownSecretsOptions());
            sut.Load();

            Assert.That(sut.Get("s1"), Is.EqualTo("value1"));
            Assert.That(sut.Get("s2"), Is.EqualTo("value2"));
        }

        [Test, CustomAutoData]
        public void JSON_is_flattened_with_GetSecretValue([Frozen] IAmazonSecretsManager secretsManager, RootObject test, IFixture fixture)
        {
            const string secretName = "my-secret";
            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, JsonSerializer.Serialize(test))
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { secretName }, new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });
            sut.Load();

            Assert.That(sut.Get(secretName, nameof(RootObject.Property)), Is.EqualTo(test.Property));
        }

        [Test, CustomAutoData]
        public void DuplicateKey_LastWins_later_id_overrides_earlier([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var r1 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s1").With(p => p.SecretString, "shared_first").Without(p => p.SecretBinary).Create();
            var r2 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s2").With(p => p.SecretString, "shared_last").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s1"), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s2"), It.IsAny<CancellationToken>())).ReturnsAsync(r2);

            const string sharedKey = "SHARED";
            var options = new SecretsManagerKnownSecretsOptions
            {
                UseBatchFetch = false,
                DuplicateKeyHandling = DuplicateKeyHandling.LastWins,
                KeyGenerator = (_, _) => sharedKey
            };

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1", "s2" }, options);
            sut.Load();

            Assert.That(sut.Get(sharedKey), Is.EqualTo("shared_last"));
        }

        [Test, CustomAutoData]
        public void DuplicateKey_FirstWins_earlier_id_wins([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var r1 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s1").With(p => p.SecretString, "shared_first").Without(p => p.SecretBinary).Create();
            var r2 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s2").With(p => p.SecretString, "shared_last").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s1"), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "s2"), It.IsAny<CancellationToken>())).ReturnsAsync(r2);

            const string sharedKey = "SHARED";
            var options = new SecretsManagerKnownSecretsOptions
            {
                UseBatchFetch = false,
                DuplicateKeyHandling = DuplicateKeyHandling.FirstWins,
                KeyGenerator = (_, _) => sharedKey
            };

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1", "s2" }, options);
            sut.Load();

            Assert.That(sut.Get(sharedKey), Is.EqualTo("shared_first"));
        }

        [Test, CustomAutoData]
        public void ConfigureSecretValueRequest_is_invoked_for_each_id([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture, string versionStage)
        {
            var r1 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s1").With(p => p.SecretString, "v1").Without(p => p.SecretBinary).Create();
            var r2 = fixture.Build<GetSecretValueResponse>().With(p => p.Name, "s2").With(p => p.SecretString, "v2").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);

            var options = new SecretsManagerKnownSecretsOptions
            {
                UseBatchFetch = false,
                ConfigureSecretValueRequest = (req, _) => req.VersionStage = versionStage
            };

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1", "s2" }, options);
            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.VersionStage == versionStage), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test, CustomAutoData]
        public void ConfigureBatchSecretValueRequest_is_invoked([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var batchResponse = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry> { new SecretValueEntry { ARN = "s1", Name = "s1", SecretString = "v1" } })
                .With(p => p.Errors, new List<APIErrorType>())
                .Without(p => p.NextToken)
                .Create();

            BatchGetSecretValueRequest? capturedRequest = null;
            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .Callback<BatchGetSecretValueRequest, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(batchResponse);

            var options = new SecretsManagerKnownSecretsOptions
            {
                ConfigureBatchSecretValueRequest = (req, _) => req.Filters = new List<Filter> { new Filter { Key = FilterNameStringType.Name, Values = new List<string> { "test" } } }
            };

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "s1" }, options);
            sut.Load();

            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Filters, Is.Not.Null);
        }

        [Test, CustomAutoData]
        public void Should_throw_on_missing_secret_with_GetSecretValue([Frozen] IAmazonSecretsManager secretsManager)
        {
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).Throws(new ResourceNotFoundException("Oops"));

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "my-secret" }, new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });

            Assert.That(sut.Load, Throws.TypeOf<MissingSecretValueException>());
        }

        [Test, CustomAutoData]
        public void Should_throw_MissingSecretValueException_when_secret_absent_from_batch_response([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            // The batch response returns "other-secret" but the configured id is "my-secret"
            var batchResponse = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry> { new SecretValueEntry { ARN = "arn:other", Name = "other-secret", SecretString = "v" } })
                .With(p => p.Errors, new List<APIErrorType>())
                .Without(p => p.NextToken)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(batchResponse);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "my-secret" }, new SecretsManagerKnownSecretsOptions());

            Assert.That(sut.Load, Throws.TypeOf<MissingSecretValueException>());
        }

        [Test, CustomAutoData]
        public void Batch_values_resolved_by_partial_ARN([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            // Caller supplies a partial ARN; the batch response returns a full ARN that starts with it
            const string partialArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret";
            const string fullArn    = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf";

            var batchResponse = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry>
                {
                    new SecretValueEntry { ARN = fullArn, Name = "my-secret", SecretString = "secret-value" }
                })
                .With(p => p.Errors, new List<APIErrorType>())
                .Without(p => p.NextToken)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(batchResponse);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { partialArn }, new SecretsManagerKnownSecretsOptions());
            sut.Load();

            Assert.That(sut.Get("my-secret"), Is.EqualTo("secret-value"));
        }

        [Test, CustomAutoData]
        public void Should_throw_on_batch_errors([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            var batchResponse = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues, new List<SecretValueEntry>())
                .With(p => p.Errors, new List<APIErrorType> { new APIErrorType { ErrorCode = nameof(ResourceNotFoundException) } })
                .Without(p => p.NextToken)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(batchResponse);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { "my-secret" }, new SecretsManagerKnownSecretsOptions());

            Assert.That(sut.Load, Throws.TypeOf<AggregateException>());
        }

        [Test, CustomAutoData]
        public void Should_poll_and_reload_when_secrets_changed([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture, Action<object?> changeCallback, object changeCallbackState)
        {
            const string secretName = "my-secret";
            var initial = fixture.Build<GetSecretValueResponse>().With(p => p.Name, secretName).With(p => p.SecretString, "initial").Without(p => p.SecretBinary).Create();
            var updated = fixture.Build<GetSecretValueResponse>().With(p => p.Name, secretName).With(p => p.SecretString, "updated").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial).ReturnsAsync(updated);

            var options = new SecretsManagerKnownSecretsOptions { UseBatchFetch = false, ReloadInterval = TimeSpan.FromMilliseconds(100) };
            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { secretName }, options);
            using var reloadEvent = new ManualResetEventSlim();
            sut.GetReloadToken().RegisterChangeCallback(state =>
            {
                changeCallback(state);
                reloadEvent.Set();
            }, changeCallbackState);

            sut.Load();
            Assert.That(sut.Get(secretName), Is.EqualTo("initial"));

            Assert.That(reloadEvent.Wait(TimeSpan.FromSeconds(5)), Is.True, "Expected reload callback to be invoked within 5 seconds.");

            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(secretName), Is.EqualTo("updated"));
        }

        [Test, CustomAutoData]
        public async Task Should_reload_when_forceReload_called([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture, Action<object?> changeCallback, object changeCallbackState)
        {
            const string secretName = "my-secret";
            var initial = fixture.Build<GetSecretValueResponse>().With(p => p.Name, secretName).With(p => p.SecretString, "initial").Without(p => p.SecretBinary).Create();
            var updated = fixture.Build<GetSecretValueResponse>().With(p => p.Name, secretName).With(p => p.SecretString, "updated").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial).ReturnsAsync(updated);

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(secretsManager, new[] { secretName }, new SecretsManagerKnownSecretsOptions { UseBatchFetch = false });
            sut.GetReloadToken().RegisterChangeCallback(changeCallback, changeCallbackState);

            sut.Load();
            Assert.That(sut.Get(secretName), Is.EqualTo("initial"));

            await sut.ForceReloadAsync(CancellationToken.None);

            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(secretName), Is.EqualTo("updated"));
        }
    }
}
