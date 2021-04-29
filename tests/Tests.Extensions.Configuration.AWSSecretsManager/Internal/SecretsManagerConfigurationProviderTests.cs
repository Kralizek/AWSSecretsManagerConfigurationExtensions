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
using System.Linq;
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
        public void Simple_values_in_string_can_be_handled([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Complex_JSON_objects_in_string_can_be_handled([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, RootObject test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Property)), Is.EqualTo(test.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Property)), Is.EqualTo(test.Mid.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Leaf), nameof(Leaf.Property)), Is.EqualTo(test.Mid.Leaf.Property));
        }

        [Test, CustomAutoData]
        public void Complex_JSON_objects_with_arrays_can_be_handled([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, RootObjectWithArray test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, nameof(RootObjectWithArray.Properties), "0"), Is.EqualTo(test.Properties[0]));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObjectWithArray.Mids), "0", nameof(MidLevel.Property)), Is.EqualTo(test.Mids[0].Property));
        }

        [Test, CustomAutoData]
        public void Array_Of_Complex_JSON_objects_with_arrays_can_be_handled([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, RootObjectWithArray[] test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                                                .Without(p => p.SecretBinary)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, "0", nameof(RootObjectWithArray.Properties), "0"), Is.EqualTo(test[0].Properties[0]));
            Assert.That(sut.Get(testEntry.Name, "0", nameof(RootObjectWithArray.Mids), "0", nameof(MidLevel.Property)), Is.EqualTo(test[0].Mids[0].Property));
            Assert.That(sut.Get(testEntry.Name, "1", nameof(RootObjectWithArray.Properties), "0"), Is.EqualTo(test[1].Properties[0]));
            Assert.That(sut.Get(testEntry.Name, "1", nameof(RootObjectWithArray.Mids), "0", nameof(MidLevel.Property)), Is.EqualTo(test[1].Mids[0].Property));
        }

        [Test, CustomAutoData]
        public void Values_in_binary_are_ignored([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretBinary)
                                                .Without(p => p.SecretString)
                                                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.HasKey(testEntry.Name), Is.False);
        }

        [Test, CustomAutoData]
        public void Secrets_can_be_filtered_out_via_options([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            options.SecretFilter = entry => false;

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.That(sut.Get(testEntry.Name), Is.Null);
        }
        
        [Test, CustomAutoData]
        public void Secrets_can_be_listed_explicitly_and_not_searched([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            const string secretKey = "KEY";
            var firstSecretArn = listSecretsResponse.SecretList.Select(x => x.ARN).First();
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(x => x.SecretId.Equals(firstSecretArn)), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);
            
            options.SecretFilter = entry => true;
            options.AcceptedSecretArns = new List<string> {firstSecretArn};
            options.KeyGenerator = (entry, key) => secretKey;

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(x => !x.SecretId.Equals(firstSecretArn)), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(secretsManager).Verify(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.That(sut.Get(testEntry.Name), Is.Null);
            Assert.That(sut.Get(secretKey), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Secrets_listed_explicitly_and_saved_to_configuration_with_their_arns_as_keys([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(x => x.SecretId.Equals(getSecretValueResponse.ARN)), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            options.AcceptedSecretArns = new List<string> { getSecretValueResponse.ARN };

            Assert.DoesNotThrow(sut.Load);

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(x => !x.SecretId.Equals(getSecretValueResponse.ARN)), It.IsAny<CancellationToken>()), Times.Never);
            
            Assert.That(sut.Get(getSecretValueResponse.ARN), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Secrets_can_be_filtered_out_via_options_on_fetching([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            options.ListSecretsFilters = new List<Filter> { new Filter { Key = FilterNameStringType.Name, Values = new List<string> { testEntry.Name } } };

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.Is<ListSecretsRequest>(request => request.Filters == options.ListSecretsFilters), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.ListSecretsAsync(It.Is<ListSecretsRequest>(request => request.Filters == options.ListSecretsFilters), It.IsAny<CancellationToken>()));

            Assert.That(sut.Get(testEntry.Name), Is.Null);
        }

        [Test, CustomAutoData]
        public void Keys_can_be_customized_via_options([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, string newKey, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            options.KeyGenerator = (entry, key) => newKey;

            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.Null);
            Assert.That(sut.Get(newKey), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        [Description("Reproduces issue 32")]
        public void Keys_should_be_case_insensitive([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name.ToLower()), Is.EqualTo(getSecretValueResponse.SecretString));
            Assert.That(sut.Get(testEntry.Name.ToUpper()), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void Should_throw_on_missing_secret_value([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).Throws(new ResourceNotFoundException("Oops"));

            Assert.That(sut.Load, Throws.TypeOf<MissingSecretValueException>());
        }

        [Test, CustomAutoData]
        public void Should_poll_and_reload_when_secrets_changed([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueInitialResponse, GetSecretValueResponse getSecretValueUpdatedResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture, Action<object> changeCallback, object changeCallbackState)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

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
        
        [Test, CustomAutoData]
        public async Task Should_reload_when_forceReload_called([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueInitialResponse, GetSecretValueResponse getSecretValueUpdatedResponse, [Frozen] IAmazonSecretsManager secretsManager, [Frozen] SecretsManagerConfigurationProviderOptions options, SecretsManagerConfigurationProvider sut, IFixture fixture, Action<object> changeCallback, object changeCallbackState)
        {
            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(getSecretValueInitialResponse)
                .ReturnsAsync(getSecretValueUpdatedResponse);

            sut.GetReloadToken().RegisterChangeCallback(changeCallback, changeCallbackState);

            sut.Load();
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueInitialResponse.SecretString));


            await sut.ForceReloadAsync(CancellationToken.None);
            
            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueUpdatedResponse.SecretString));
        }
        
        [Test]
        [Description("Reproduces issue 48")]
        [CustomInlineAutoData("{THIS IS NOT AN OBJECT}")]
        [CustomInlineAutoData("[THIS IS NOT AN ARRAY]")]
        public void Incorrect_json_should_be_processed_as_string(string content, [Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, GetSecretValueResponse getSecretValueResponse, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut)
        {
            getSecretValueResponse.SecretString = content;

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, CustomAutoData]
        public void JSON_with_leading_spaces_should_be_processed_as_JSON([Frozen] SecretListEntry testEntry, ListSecretsResponse listSecretsResponse, RootObject test, [Frozen] IAmazonSecretsManager secretsManager, SecretsManagerConfigurationProvider sut, IFixture fixture)
        {
            var secretString = " " + JsonConvert.SerializeObject(test);

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                .With(p => p.SecretString, secretString)
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(listSecretsResponse);

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Property)), Is.EqualTo(test.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Property)), Is.EqualTo(test.Mid.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Leaf), nameof(Leaf.Property)), Is.EqualTo(test.Mid.Leaf.Property));
        }
    }
}