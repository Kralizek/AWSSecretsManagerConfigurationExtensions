using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AutoFixture;
using AutoFixture.NUnit3;
using Kralizek.Extensions.Configuration.Internal;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tests.Types;

namespace Tests.Internal
{
    [TestFixture]
    [TestOf(typeof(SecretsManagerConfigurationProvider))]
    public class SecretsManagerConfigurationProviderTests
    {
        [Test, CustomAutoData]
        public void Simple_values_in_string_can_be_handled(SecretListEntry testEntry, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString)
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Complex_JSON_objects_in_string_can_be_handled(SecretListEntry testEntry, RootObject test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Property)), Is.EqualTo(test.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Property)), Is.EqualTo(test.Mid.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Leaf), nameof(Leaf.Property)), Is.EqualTo(test.Mid.Leaf.Property));
        }

        [Test, CustomAutoData]
        public void Complex_JSON_objects_with_arrays_can_be_handled(SecretListEntry testEntry, RootObjectWithArray test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, nameof(RootObjectWithArray.Properties), "0"), Is.EqualTo(test.Properties[0]));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObjectWithArray.Mids), "0", nameof(MidLevel.Property)), Is.EqualTo(test.Mids[0].Property));
        }

        [Test, CustomAutoData]
        public void Array_Of_Complex_JSON_objects_with_arrays_can_be_handled(SecretListEntry testEntry, RootObjectWithArray[] test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                .With(p => p.NextToken, null)
                .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, "0", nameof(RootObjectWithArray.Properties), "0"), Is.EqualTo(test[0].Properties[0]));
            Assert.That(sut.Get(testEntry.Name, "0", nameof(RootObjectWithArray.Mids), "0", nameof(MidLevel.Property)), Is.EqualTo(test[0].Mids[0].Property));
            Assert.That(sut.Get(testEntry.Name, "1", nameof(RootObjectWithArray.Properties), "0"), Is.EqualTo(test[1].Properties[0]));
            Assert.That(sut.Get(testEntry.Name, "1", nameof(RootObjectWithArray.Mids), "0", nameof(MidLevel.Property)), Is.EqualTo(test[1].Mids[0].Property));
        }

        [Test, CustomAutoData]
        public void Values_in_binary_are_ignored(SecretListEntry testEntry, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretBinary)
                                                .Without(p => p.SecretString)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.HasKey(testEntry.Name), Is.False);
        }

        [Test, CustomAutoData]
        public void Secrets_can_be_filtered_out_via_options(SecretListEntry testEntry, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            options.SecretFilter = entry => false;

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.That(sut.Get(testEntry.Name), Is.Null);
        }

        [Test, CustomAutoData]
        public void Keys_can_be_customized_via_options(SecretListEntry testEntry, string newKey, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString)
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            options.KeyGenerator = (entry, key) => newKey;

            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.Null);
            Assert.That(sut.Get(newKey), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        [Description("Reproduces issue 32")]
        public void Keys_should_be_case_insesitive(SecretListEntry testEntry, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString)
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name.ToLower()), Is.EqualTo(getSecretValueResponse.SecretString));
            Assert.That(sut.Get(testEntry.Name.ToUpper()), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Should_throw_on_missing_secret_value(SecretListEntry testEntry, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();


            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).Throws(new ResourceNotFoundException("Oops"));

            Assert.That(() => sut.Load(), Throws.TypeOf<MissingSecretValueException>());
        }

        [Test, CustomAutoData]
        public void Should_poll_and_reload_when_secrets_changed(SecretListEntry testEntry, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture, Action<object> changeCallback, object changeCallbackState)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueInitialResponse = fixture.Build<GetSecretValueResponse>()
                                                       .With(p => p.SecretString)
                                                       .Without(p => p.SecretBinary)
                                                       .Create();

            var getSecretValueUpdatedResponse = fixture.Build<GetSecretValueResponse>()
                                                       .With(p => p.SecretString)
                                                       .Without(p => p.SecretBinary)
                                                       .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                              .ReturnsAsync(getSecretValueInitialResponse)
                              .ReturnsAsync(getSecretValueUpdatedResponse);

            options.PollingInterval = TimeSpan.FromMilliseconds(100);

            sut.GetReloadToken().RegisterChangeCallback(changeCallback, changeCallbackState);

            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueInitialResponse.SecretString));

            Thread.Sleep(200);

            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueUpdatedResponse.SecretString));
        }
    }
}