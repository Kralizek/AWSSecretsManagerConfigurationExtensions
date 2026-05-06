using System;
using System.Collections.Generic;
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
    [TestOf(typeof(SecretsManagerKnownSecretConfigurationProvider))]
    public class SecretsManagerKnownSecretConfigurationProviderTests
    {
        [Test, CustomAutoData]
        public void Never_calls_ListSecrets([Frozen] IAmazonSecretsManager secretsManager, GetSecretValueResponse getSecretValueResponse, IFixture fixture)
        {
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, "my-secret", new SecretsManagerKnownSecretOptions());
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test, CustomAutoData]
        public void Never_calls_BatchGetSecretValue([Frozen] IAmazonSecretsManager secretsManager, GetSecretValueResponse getSecretValueResponse, IFixture fixture)
        {
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, "my-secret", new SecretsManagerKnownSecretOptions());
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test, CustomAutoData]
        public void Calls_GetSecretValue_with_configured_secretId([Frozen] IAmazonSecretsManager secretsManager, GetSecretValueResponse getSecretValueResponse)
        {
            const string secretId = "my-known-secret";
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == secretId), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == secretId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, CustomAutoData]
        public void Simple_string_secret_loaded_with_response_name_as_key([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string secretId = "my-secret";
            const string secretName = "my-secret-name";
            const string secretValue = "the-value";

            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, secretValue)
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretName), Is.EqualTo(secretValue));
        }

        [Test, CustomAutoData]
        public void Binary_secret_is_ignored([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string secretId = "my-secret";
            const string secretName = "my-secret";

            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretBinary)
                .Without(p => p.SecretString)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.HasKey(secretName), Is.False);
        }

        [Test, CustomAutoData]
        public void JSON_secret_is_flattened([Frozen] IAmazonSecretsManager secretsManager, RootObject test, IFixture fixture)
        {
            const string secretId = "my-secret";
            const string secretName = "my-secret-name";

            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, secretName)
                .With(p => p.SecretString, JsonSerializer.Serialize(test))
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretName, nameof(RootObject.Property)), Is.EqualTo(test.Property));
        }

        [Test, CustomAutoData]
        public void Key_is_rooted_at_response_Name_not_request_secretId([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string secretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:App-AbCdEf";
            const string secretName = "/App/Production/Config";
            const string secretJson = "{\"Key\":\"value\"}";

            var response = new GetSecretValueResponse
            {
                ARN = secretArn,
                Name = secretName,
                SecretString = secretJson
            };

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretArn, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretName, "Key"), Is.EqualTo("value"));
            Assert.That(sut.HasKey(secretArn, "Key"), Is.False);
        }

        [Test, CustomAutoData]
        [Description("Regression: known secretId with JSON flattening must root keys at the actual secret Name returned by Secrets Manager, not the requested ARN/id")]
        public void KnownSecret_with_json_flattening_roots_keys_at_secret_name_not_arn([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string secretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:App-Production-Service-Settings-Nested-Section-AbCdEf";
            const string secretName = "/App/Production/Service/Settings/Nested/Section";
            const string secretJson = "{\"Property\":\"value\",\"Nested\":{\"Enabled\":true}}";
            const string pathPrefix = "/App/Production/Service/Settings/";

            var response = new GetSecretValueResponse
            {
                ARN = secretArn,
                Name = secretName,
                SecretString = secretJson
            };

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == secretArn), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var options = new SecretsManagerKnownSecretOptions
            {
                KeyGenerator = (_, key) =>
                {
                    var stripped = key.StartsWith(pathPrefix) ? key.Substring(pathPrefix.Length) : key;
                    return stripped.Replace("/", ":");
                }
            };

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretArn, options);
            sut.Load();

            Assert.That(sut.Get("Nested:Section:Property"), Is.EqualTo("value"));
            Assert.That(sut.Get("Nested:Section:Nested:Enabled"), Is.EqualTo("True"));
        }

        [Test, CustomAutoData]
        public void Key_generator_is_applied([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture)
        {
            const string customKey = "CUSTOM_KEY";
            var response = fixture.Build<GetSecretValueResponse>()
                .With(p => p.Name, "my-secret")
                .With(p => p.SecretString, "value")
                .Without(p => p.SecretBinary)
                .Create();

            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var options = new SecretsManagerKnownSecretOptions { KeyGenerator = (_, _) => customKey };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, "my-secret", options);
            sut.Load();

            Assert.That(sut.Get(customKey), Is.EqualTo("value"));
        }

        [Test, CustomAutoData]
        public void ConfigureSecretValueRequest_is_invoked([Frozen] IAmazonSecretsManager secretsManager, GetSecretValueResponse response, string versionStage)
        {
            response.SecretString = "val";
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

            var options = new SecretsManagerKnownSecretOptions
            {
                ConfigureSecretValueRequest = (req, _) => req.VersionStage = versionStage
            };

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, "my-secret", options);
            sut.Load();

            Mock.Get(secretsManager).Verify(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.VersionStage == versionStage), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test, CustomAutoData]
        public void Should_throw_on_missing_secret([Frozen] IAmazonSecretsManager secretsManager)
        {
            Mock.Get(secretsManager).Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).Throws(new ResourceNotFoundException("Oops"));

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, "my-secret", new SecretsManagerKnownSecretOptions());

            Assert.That(sut.Load, Throws.TypeOf<MissingSecretValueException>());
        }

        [Test, CustomAutoData]
        public void Should_poll_and_reload_when_secret_changed([Frozen] IAmazonSecretsManager secretsManager, IFixture fixture, Action<object?> changeCallback, object changeCallbackState)
        {
            const string secretName = "my-secret";
            var initial = fixture.Build<GetSecretValueResponse>().With(p => p.Name, secretName).With(p => p.SecretString, "initial").Without(p => p.SecretBinary).Create();
            var updated = fixture.Build<GetSecretValueResponse>().With(p => p.Name, secretName).With(p => p.SecretString, "updated").Without(p => p.SecretBinary).Create();

            Mock.Get(secretsManager).SetupSequence(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(initial).ReturnsAsync(updated);

            var options = new SecretsManagerKnownSecretOptions { ReloadInterval = TimeSpan.FromMilliseconds(100) };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretName, options);
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

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretName, new SecretsManagerKnownSecretOptions());
            sut.GetReloadToken().RegisterChangeCallback(changeCallback, changeCallbackState);

            sut.Load();
            Assert.That(sut.Get(secretName), Is.EqualTo("initial"));

            await sut.ForceReloadAsync(CancellationToken.None);

            Mock.Get(changeCallback).Verify(c => c(changeCallbackState));
            Assert.That(sut.Get(secretName), Is.EqualTo("updated"));
        }

        // #103 – AWS SDK v4 compatibility: GetSecretValueResponse.CreatedDate is DateTime? in v4.
        // The provider must not read CreatedDate; these tests prove that neither null nor a
        // populated CreatedDate affects whether the secret value is loaded.

        [Test, CustomAutoData]
        [Description("#103: A v4-shaped GetSecretValueResponse with CreatedDate = null must not break loading.")]
        public void Load_succeeds_with_v4_shaped_response_with_CreatedDate_null(
            [Frozen] IAmazonSecretsManager secretsManager)
        {
            const string secretId = "my-secret";
            var response = new GetSecretValueResponse
            {
                ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf",
                Name = secretId,
                SecretString = "the-value",
                CreatedDate = null   // v4: nullable – may be absent from the API response
            };

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretId), Is.EqualTo("the-value"));
        }

        [Test, CustomAutoData]
        [Description("#103: A v4-shaped GetSecretValueResponse with CreatedDate = DateTime.UtcNow must not break loading.")]
        public void Load_succeeds_with_v4_shaped_response_with_CreatedDate_set(
            [Frozen] IAmazonSecretsManager secretsManager)
        {
            const string secretId = "my-secret";
            var response = new GetSecretValueResponse
            {
                ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf",
                Name = secretId,
                SecretString = "the-value",
                CreatedDate = DateTime.UtcNow   // v4: nullable with a value
            };

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretId, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretId), Is.EqualTo("the-value"));
        }

        // #101 – Known secret identifier matching: the single-KnownSecret provider passes
        // the configured secretId directly to GetSecretValue, so it works with any identifier
        // shape (name, full ARN, partial ARN) that the Secrets Manager API accepts.

        [Test, CustomAutoData]
        [Description("#101: Configured by full ARN → response keys are rooted at the response Name.")]
        public void Load_by_full_ARN_roots_keys_at_response_Name(
            [Frozen] IAmazonSecretsManager secretsManager)
        {
            const string fullArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf";
            const string secretName = "my-secret";

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == fullArn), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { ARN = fullArn, Name = secretName, SecretString = "secret-value" });

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, fullArn, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretName), Is.EqualTo("secret-value"));
            // The full ARN must NOT be used as the config key root.
            Assert.That(sut.HasKey(fullArn), Is.False);
        }

        [Test, CustomAutoData]
        [Description("#101: Configured by secret name → the same name is used both as request id and config key root.")]
        public void Load_by_name_uses_response_Name_as_config_key_root(
            [Frozen] IAmazonSecretsManager secretsManager)
        {
            const string secretName = "my-secret";

            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == secretName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf", Name = secretName, SecretString = "secret-value" });

            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, secretName, new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get(secretName), Is.EqualTo("secret-value"));
        }
    }
}