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

        // #65 – replaced the trivially-passing "Secrets_can_be_filtered_out_via_options" test with two
        // focused tests that exercise both the excluded and the included path, ensuring neither passes
        // vacuously (i.e. both tests set up a real GetSecretValue response so the value *would* appear
        // in configuration if the filter were not applied).

        [Test, CustomAutoData]
        [Description("#65: A secret excluded by SecretFilter must not appear in configuration, and GetSecretValue must never be called for it.")]
        public void SecretFilter_excluding_all_secrets_means_no_secrets_are_loaded(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            // Set up a real value – without the filter the secret *would* be loaded.
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            options.SecretFilter = _ => false;
            sut.Load();

            // Filter prevents the fetch entirely.
            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            // And therefore the key is absent from configuration.
            Assert.That(sut.Get(testEntry.Name), Is.Null);
        }

        [Test, CustomAutoData]
        [Description("#65: A secret that passes SecretFilter must appear in configuration (positive-control test).")]
        public void SecretFilter_accepting_a_secret_means_it_is_loaded(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            GetSecretValueResponse getSecretValueResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut)
        {
            options.UseBatchFetch = false;
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            options.SecretFilter = _ => true;
            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        [Description("#65: ListSecretsFilters configured on the options must be forwarded as-is to the ListSecrets API call.")]
        public void ListSecretsFilters_are_forwarded_to_the_ListSecrets_API(
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options,
            SecretsManagerDiscoveryConfigurationProvider sut,
            IFixture fixture)
        {
            var expectedFilter = new Filter { Key = FilterNameStringType.Name, Values = new List<string> { "my-prefix" } };
            options.ListSecretsFilters.Add(expectedFilter);

            ListSecretsRequest? capturedRequest = null;
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ListSecretsRequest, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry>() });

            sut.Load();

            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Filters, Does.Contain(expectedFilter));
        }

        [Test, CustomAutoData]
        public void Batch_mode_logs_SecretSkipped_for_filtered_secrets([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerDiscoveryOptions options, SecretsManagerDiscoveryConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            options.UseBatchFetch = true;
            options.SecretFilter = entry => false;

            var loggedEvents = new List<SecretsManagerLogEvent>();
            options.LogEvent = e => loggedEvents.Add(e);

            sut.Load();

            Assert.That(loggedEvents, Has.Some.Matches<SecretsManagerLogEvent>(
                e => e.EventId == SecretsManagerLogEvents.SecretSkipped));
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

        // #103 – AWS SDK v4 compatibility: GetSecretValueResponse.CreatedDate is DateTime? in v4.
        // The provider must never read CreatedDate; these tests prove that both null and non-null
        // values of CreatedDate do not affect provider loading.

        [Test, CustomAutoData]
        [Description("#103: A response with CreatedDate = null must not break provider loading.")]
        public void Load_succeeds_when_GetSecretValueResponse_has_null_CreatedDate(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options)
        {
            options.UseBatchFetch = false;
            var response = new GetSecretValueResponse
            {
                ARN = testEntry.ARN,
                Name = testEntry.Name,
                SecretString = "the-value",
                CreatedDate = null   // v4: nullable DateTime
            };
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo("the-value"));
        }

        [Test, CustomAutoData]
        [Description("#103: A response with CreatedDate = DateTime.UtcNow must not break provider loading.")]
        public void Load_succeeds_when_GetSecretValueResponse_has_non_null_CreatedDate(
            [Frozen] SecretListEntry testEntry,
            ListSecretsResponse listSecretsResponse,
            [Frozen] IAmazonSecretsManager secretsManager,
            [Frozen] SecretsManagerDiscoveryOptions options)
        {
            options.UseBatchFetch = false;
            var response = new GetSecretValueResponse
            {
                ARN = testEntry.ARN,
                Name = testEntry.Name,
                SecretString = "the-value",
                CreatedDate = DateTime.UtcNow   // v4: nullable DateTime with a value
            };
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo("the-value"));
        }

        // #82/#20 – Deleted / scheduled-for-deletion secrets.
        // SecretListEntry.DeletedDate is set when a secret is scheduled for deletion.
        // The provider does NOT automatically filter such secrets: it is the caller's
        // responsibility to exclude them via SecretFilter (or ListSecretsFilters).
        // These tests document that behavior explicitly.

        [Test, CustomAutoData]
        [Description("#82: Discovery does NOT automatically skip secrets whose DeletedDate is set; callers must filter via SecretFilter.")]
        public void Discovery_does_not_auto_skip_secrets_scheduled_for_deletion(
            [Frozen] IAmazonSecretsManager secretsManager,
            IFixture fixture)
        {
            var deletedSecret = new SecretListEntry
            {
                ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf",
                Name = "my-secret",
                DeletedDate = DateTime.UtcNow.AddDays(-2)  // scheduled for deletion
            };

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { deletedSecret } });
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { ARN = deletedSecret.ARN, Name = deletedSecret.Name, SecretString = "should-not-reach-here" });

            var options = new SecretsManagerDiscoveryOptions { UseBatchFetch = false };
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            // Without an explicit filter the secret is fetched; the value IS loaded.
            Assert.That(sut.Get("my-secret"), Is.EqualTo("should-not-reach-here"));
        }

        [Test, CustomAutoData]
        [Description("#82: Callers can use SecretFilter to exclude secrets whose DeletedDate is set.")]
        public void Discovery_can_exclude_secrets_scheduled_for_deletion_via_SecretFilter(
            [Frozen] IAmazonSecretsManager secretsManager)
        {
            var deletedSecret = new SecretListEntry
            {
                ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf",
                Name = "my-secret",
                DeletedDate = DateTime.UtcNow.AddDays(-2)
            };

            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { deletedSecret } });

            // Filter that callers are expected to apply to skip deletion-scheduled secrets.
            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = false,
                SecretFilter = entry => entry.DeletedDate == null
            };
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.That(sut.Get("my-secret"), Is.Null);
        }
    }
}