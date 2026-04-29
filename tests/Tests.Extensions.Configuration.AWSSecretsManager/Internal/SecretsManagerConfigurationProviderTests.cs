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
    [TestOf(typeof(SecretsManagerDiscoveryConfigurationProvider))]
    public class SecretsManagerConfigurationProviderTests
    {
        [Test, CustomAutoData]
        public void Simple_values_in_string_can_be_handled([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);
            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Values_in_binary_are_ignored([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            options.UseBatchFetch = false;
            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                .With(p => p.SecretBinary).Without(p => p.SecretString).Create();
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);
            sut.Load();
            Assert.That(sut.HasKey(testEntry.Name), Is.False);
        }

        [Test, CustomAutoData]
        public void Secrets_can_be_filtered_out_via_options([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            options.SecretFilter = entry => false;
            sut.Load();
            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(sut.Get(testEntry.Name), Is.Null);
        }

        [Test, CustomAutoData]
        public void Keys_can_be_customized_via_options([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, string newKey, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);
            options.KeyGenerator = (entry, key) => newKey;
            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.Null);
            Assert.That(sut.Get(newKey), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Get_secret_value_request_can_be_customized_via_options([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, string secretVersionStage, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);
            options.ConfigureSecretValueRequest = (request, _) => request.VersionStage = secretVersionStage;
            sut.Load();
            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(x => x.VersionStage == secretVersionStage), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, CustomAutoData]
        public void Should_throw_on_missing_secret_value([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).Throws(new ResourceNotFoundException("Oops"));
            Assert.That(sut.Load, Throws.TypeOf<MissingSecretValueException>());
        }

        [Test, CustomAutoData]
        public void Should_throw_on_batch_missing_secret_values([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            var batchResp = fixture.Build<BatchGetSecretValueResponse>()
                .With(p => p.SecretValues)
                .With(p => p.Errors, new List<APIErrorType> { new APIErrorType { ErrorCode = nameof(ResourceNotFoundException) } })
                .Without(p => p.NextToken).Create();
            Mock.Get(secretsManager).Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(batchResp);
            options.UseBatchFetch = true;
            Assert.That(sut.Load, Throws.TypeOf<AggregateException>());
        }

        [Test, CustomAutoData]
        public void Should_poll_and_reload_when_secrets_changed([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse initial, GetSecretValueResponse updated, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture, Action<object?> changeCallback, object changeCallbackState)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial).ReturnsAsync(updated);
            options.ReloadInterval = TimeSpan.FromMilliseconds(100);
            using var reloadEvent = new ManualResetEventSlim();
            sut.GetReloadToken().RegisterChangeCallback(state =>
            {
                changeCallback(state);
                reloadEvent.Set();
            }, changeCallbackState);
            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(initial.SecretString));
            Assert.That(reloadEvent.Wait(TimeSpan.FromSeconds(5)), Is.True, "Expected reload callback to be invoked within 5 seconds.");
            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(updated.SecretString));
        }

        [Test, CustomAutoData]
        public async Task Should_reload_when_forceReload_called([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse initial, GetSecretValueResponse updated, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture, Action<object?> changeCallback, object changeCallbackState)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial).ReturnsAsync(updated);
            sut.GetReloadToken().RegisterChangeCallback(changeCallback, changeCallbackState);
            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(initial.SecretString));
            await sut.ForceReloadAsync(CancellationToken.None);
            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(updated.SecretString));
        }

        [Test]
        [CustomInlineAutoData("{THIS IS NOT AN OBJECT}")]
        [CustomInlineAutoData("[THIS IS NOT AN ARRAY]")]
        public void Incorrect_json_should_be_processed_as_string(string content, [Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            getSecretValueResponse.SecretString = content;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);
            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void DuplicateKey_Throw_throws([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string duplicateKey = "DUPLICATE_KEY";
            var s1 = new SecretListEntry { ARN = "arn1", Name = "s1" };
            var s2 = new SecretListEntry { ARN = "arn2", Name = "s2" };
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { s1, s2 } });
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn1"), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "v1").Without(p => p.SecretBinary).Create());
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn2"), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "v2").Without(p => p.SecretBinary).Create());
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, new SecretsManagerDiscoveryOptions { DuplicateKeyHandling = DuplicateKeyHandling.Throw, KeyGenerator = (_, _) => duplicateKey, UseBatchFetch = false });
            Assert.That(sut.Load, Throws.TypeOf<InvalidOperationException>());
        }

        [Test, CustomAutoData]
        public void DuplicateKey_FirstWins_keeps_first_value([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string duplicateKey = "DUPLICATE_KEY";
            var s1 = new SecretListEntry { ARN = "arn1", Name = "s1" };
            var s2 = new SecretListEntry { ARN = "arn2", Name = "s2" };
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { s1, s2 } });
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn1"), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "first").Without(p => p.SecretBinary).Create());
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn2"), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "second").Without(p => p.SecretBinary).Create());
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, new SecretsManagerDiscoveryOptions { DuplicateKeyHandling = DuplicateKeyHandling.FirstWins, KeyGenerator = (_, _) => duplicateKey, UseBatchFetch = false });
            sut.Load();
            Assert.That(sut.Get(duplicateKey), Is.EqualTo("first"));
        }

        [Test, CustomAutoData]
        public void DuplicateKey_LastWins_keeps_last_value([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string duplicateKey = "DUPLICATE_KEY";
            var s1 = new SecretListEntry { ARN = "arn1", Name = "s1" };
            var s2 = new SecretListEntry { ARN = "arn2", Name = "s2" };
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { s1, s2 } });
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn1"), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "first").Without(p => p.SecretBinary).Create());
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "arn2"), It.IsAny<CancellationToken>())).ReturnsAsync(fixture.Build<GetSecretValueResponse>().With(p => p.SecretString, "last").Without(p => p.SecretBinary).Create());
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, new SecretsManagerDiscoveryOptions { DuplicateKeyHandling = DuplicateKeyHandling.LastWins, KeyGenerator = (_, _) => duplicateKey, UseBatchFetch = false });
            sut.Load();
            Assert.That(sut.Get(duplicateKey), Is.EqualTo("last"));
        }
    }
}
