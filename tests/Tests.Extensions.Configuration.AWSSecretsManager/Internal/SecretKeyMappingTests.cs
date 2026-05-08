using System.Collections.Generic;
using System.Threading;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;

using Moq;

using NUnit.Framework;

namespace Tests.Internal
{
    [TestFixture]
    [TestOf(typeof(SecretKeyMapper))]
    public class SecretKeyMapperTests
    {
        // ---------------------------------------------------------------------------
        // ValidateOptions
        // ---------------------------------------------------------------------------

        [Test]
        public void ValidateOptions_does_not_throw_when_separator_is_slash()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = "/" };
            Assert.DoesNotThrow(() => SecretKeyMapper.ValidateOptions(options));
        }

        [Test]
        public void ValidateOptions_does_not_throw_when_separator_is_null()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = null };
            Assert.DoesNotThrow(() => SecretKeyMapper.ValidateOptions(options));
        }

        [Test]
        public void ValidateOptions_throws_when_separator_is_empty_string()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = string.Empty };
            Assert.Throws<System.InvalidOperationException>(() => SecretKeyMapper.ValidateOptions(options));
        }

        // ---------------------------------------------------------------------------
        // MapScalarKey — SecretNamePathSeparator
        // ---------------------------------------------------------------------------

        [Test]
        public void MapScalarKey_replaces_slash_separator_with_colon()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = "/" };
            var result = SecretKeyMapper.MapScalarKey("/my-app/production/database", options);
            Assert.That(result, Is.EqualTo("my-app:production:database"));
        }

        [Test]
        public void MapScalarKey_replaces_double_dash_separator_with_colon()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = "--" };
            var result = SecretKeyMapper.MapScalarKey("my-app--production--database", options);
            Assert.That(result, Is.EqualTo("my-app:production:database"));
        }

        [Test]
        public void MapScalarKey_trims_leading_delimiter_from_path_style_name()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = "/" };
            var result = SecretKeyMapper.MapScalarKey("/leading-slash", options);
            Assert.That(result, Is.EqualTo("leading-slash"));
        }

        [Test]
        public void MapScalarKey_leaves_name_unchanged_when_separator_is_null()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = null };
            var result = SecretKeyMapper.MapScalarKey("/my-app/production/database", options);
            Assert.That(result, Is.EqualTo("/my-app/production/database"));
        }

        [Test]
        public void MapScalarKey_leaves_simple_name_unchanged()
        {
            var options = new SecretKeyMappingOptions { SecretNamePathSeparator = "/" };
            var result = SecretKeyMapper.MapScalarKey("my-secret", options);
            Assert.That(result, Is.EqualTo("my-secret"));
        }

        // ---------------------------------------------------------------------------
        // MapScalarKey — TargetSection
        // ---------------------------------------------------------------------------

        [Test]
        public void MapScalarKey_prepends_target_section()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = "/",
                TargetSection = "Secrets"
            };
            var result = SecretKeyMapper.MapScalarKey("/my-app/production/database", options);
            Assert.That(result, Is.EqualTo("Secrets:my-app:production:database"));
        }

        [Test]
        public void MapScalarKey_does_not_prepend_null_target_section()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = "/",
                TargetSection = null
            };
            var result = SecretKeyMapper.MapScalarKey("my-secret", options);
            Assert.That(result, Is.EqualTo("my-secret"));
        }

        // ---------------------------------------------------------------------------
        // MapJsonKey — PrefixJsonKeysWithSecretName
        // ---------------------------------------------------------------------------

        [Test]
        public void MapJsonKey_includes_mapped_secret_name_prefix_when_prefix_is_true()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = "/",
                PrefixJsonKeysWithSecretName = true
            };
            // raw key = secretName:jsonPath produced by ExtractValues
            var result = SecretKeyMapper.MapJsonKey(
                "/my-app/production/database:ConnectionStrings:Database",
                "/my-app/production/database",
                options);
            Assert.That(result, Is.EqualTo("my-app:production:database:ConnectionStrings:Database"));
        }

        [Test]
        public void MapJsonKey_omits_secret_name_prefix_when_prefix_is_false()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = "/",
                PrefixJsonKeysWithSecretName = false
            };
            var result = SecretKeyMapper.MapJsonKey(
                "/my-app/production/database:ConnectionStrings:Database",
                "/my-app/production/database",
                options);
            Assert.That(result, Is.EqualTo("ConnectionStrings:Database"));
        }

        [Test]
        public void MapJsonKey_leaves_raw_key_unchanged_when_separator_is_null_and_prefix_is_true()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = null,
                PrefixJsonKeysWithSecretName = true
            };
            var result = SecretKeyMapper.MapJsonKey(
                "/my-app/production/database:ConnectionStrings:Database",
                "/my-app/production/database",
                options);
            Assert.That(result, Is.EqualTo("/my-app/production/database:ConnectionStrings:Database"));
        }

        // ---------------------------------------------------------------------------
        // MapJsonKey — TargetSection
        // ---------------------------------------------------------------------------

        [Test]
        public void MapJsonKey_prepends_target_section_with_prefix_true()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = "/",
                PrefixJsonKeysWithSecretName = true,
                TargetSection = "Secrets"
            };
            var result = SecretKeyMapper.MapJsonKey(
                "/my-app/production/database:ConnectionStrings:Database",
                "/my-app/production/database",
                options);
            Assert.That(result, Is.EqualTo("Secrets:my-app:production:database:ConnectionStrings:Database"));
        }

        [Test]
        public void MapJsonKey_prepends_target_section_with_prefix_false()
        {
            var options = new SecretKeyMappingOptions
            {
                SecretNamePathSeparator = "/",
                PrefixJsonKeysWithSecretName = false,
                TargetSection = "Secrets"
            };
            var result = SecretKeyMapper.MapJsonKey(
                "/my-app/production/database:ConnectionStrings:Database",
                "/my-app/production/database",
                options);
            Assert.That(result, Is.EqualTo("Secrets:ConnectionStrings:Database"));
        }
    }

    // ---------------------------------------------------------------------------
    // Provider integration tests for key mapping
    // ---------------------------------------------------------------------------

    [TestFixture]
    public class KeyMappingKnownSecretProviderTests
    {
        private static GetSecretValueResponse BuildJsonResponse(string secretName, string secretJson) =>
            new GetSecretValueResponse
            {
                ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test",
                Name = secretName,
                SecretString = secretJson
            };

        private static GetSecretValueResponse BuildScalarResponse(string secretName, string secretValue) =>
            new GetSecretValueResponse
            {
                ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test",
                Name = secretName,
                SecretString = secretValue
            };

        [Test]
        public void JSON_secret_with_path_name_uses_colon_separator_by_default()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildJsonResponse("/my-app/production/database", "{\"Smtp\":{\"Host\":\"localhost\"}}"));

            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/production/database", new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get("my-app:production:database:Smtp:Host"), Is.EqualTo("localhost"));
        }

        [Test]
        public void JSON_secret_without_name_prefix_uses_only_json_path()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildJsonResponse("/my-app/production/database", "{\"Smtp\":{\"Host\":\"localhost\"}}"));

            var options = new SecretsManagerKnownSecretOptions
            {
                KeyMapping = { PrefixJsonKeysWithSecretName = false }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/production/database", options);
            sut.Load();

            Assert.That(sut.Get("Smtp:Host"), Is.EqualTo("localhost"));
        }

        [Test]
        public void JSON_secret_with_target_section_and_no_prefix()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildJsonResponse("my-app/email", "{\"Smtp\":{\"Host\":\"smtp.example.com\",\"Port\":587}}"));

            var options = new SecretsManagerKnownSecretOptions
            {
                KeyMapping =
                {
                    PrefixJsonKeysWithSecretName = false,
                    TargetSection = "Email"
                }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "my-app/email", options);
            sut.Load();

            Assert.That(sut.Get("Email:Smtp:Host"), Is.EqualTo("smtp.example.com"));
            Assert.That(sut.Get("Email:Smtp:Port"), Is.EqualTo("587"));
        }

        [Test]
        public void JSON_secret_with_target_section_and_prefix()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildJsonResponse("/my-app/production/database", "{\"ConnectionStrings\":{\"Database\":\"server=.;\"}}"));

            var options = new SecretsManagerKnownSecretOptions
            {
                KeyMapping =
                {
                    PrefixJsonKeysWithSecretName = true,
                    TargetSection = "Secrets"
                }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/production/database", options);
            sut.Load();

            Assert.That(sut.Get("Secrets:my-app:production:database:ConnectionStrings:Database"), Is.EqualTo("server=.;"));
        }

        [Test]
        public void Scalar_secret_with_path_name_uses_colon_separator()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildScalarResponse("/my-app/production/database", "secret-value"));

            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/production/database", new SecretsManagerKnownSecretOptions());
            sut.Load();

            Assert.That(sut.Get("my-app:production:database"), Is.EqualTo("secret-value"));
        }

        [Test]
        public void Scalar_secret_with_target_section()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildScalarResponse("/my-app/production/database", "secret-value"));

            var options = new SecretsManagerKnownSecretOptions
            {
                KeyMapping = { TargetSection = "Secrets" }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/production/database", options);
            sut.Load();

            Assert.That(sut.Get("Secrets:my-app:production:database"), Is.EqualTo("secret-value"));
        }

        [Test]
        public void Scalar_PrefixJsonKeysWithSecretName_does_not_affect_scalar_key()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildScalarResponse("/my-app/production/database", "secret-value"));

            // PrefixJsonKeysWithSecretName does not affect scalar secrets; the key is always the mapped secret name.
            var optionsWithPrefix = new SecretsManagerKnownSecretOptions
            {
                KeyMapping = { PrefixJsonKeysWithSecretName = false }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/production/database", optionsWithPrefix);
            sut.Load();

            Assert.That(sut.Get("my-app:production:database"), Is.EqualTo("secret-value"));
        }

        [Test]
        public void Key_mapping_produces_raw_key_as_context_raw_key_and_mapped_key_as_default_key()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildJsonResponse("/my-app/database", "{\"Property\":\"value\"}"));

            SecretKeyGeneratorContext? capturedContext = null;
            var options = new SecretsManagerKnownSecretOptions
            {
                KeyGenerator = ctx => { capturedContext = ctx; return ctx.DefaultKey; }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(
                secretsManager, "/my-app/database", options);
            sut.Load();

            Assert.That(capturedContext, Is.Not.Null);
            Assert.That(capturedContext!.RawKey, Is.EqualTo("/my-app/database:Property"));
            Assert.That(capturedContext.DefaultKey, Is.EqualTo("my-app:database:Property"));
            Assert.That(capturedContext.JsonPath, Is.EqualTo("Property"));
        }

        [Test]
        public void Empty_SecretNamePathSeparator_throws_on_load()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            Mock.Get(secretsManager)
                .Setup(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(BuildScalarResponse("my-secret", "value"));

            var options = new SecretsManagerKnownSecretOptions
            {
                KeyMapping = { SecretNamePathSeparator = string.Empty }
            };
            var sut = new SecretsManagerKnownSecretConfigurationProvider(secretsManager, "my-secret", options);

            Assert.Throws<System.InvalidOperationException>(() => sut.Load());
        }
    }

    [TestFixture]
    public class KeyMappingKnownSecretsProviderTests
    {
        private static void SetupBatchResponse(IAmazonSecretsManager secretsManager, string secretName, string secretValue)
        {
            Mock.Get(secretsManager)
                .Setup(s => s.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchGetSecretValueResponse
                {
                    SecretValues = new List<SecretValueEntry>
                    {
                        new SecretValueEntry
                        {
                            ARN = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test",
                            Name = secretName,
                            SecretString = secretValue
                        }
                    },
                    Errors = new List<APIErrorType>()
                });
        }

        [Test]
        public void JSON_secret_with_path_name_uses_colon_separator()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupBatchResponse(secretsManager, "/my-app/production/database", "{\"Key\":\"value\"}");

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(
                secretsManager,
                new[] { "/my-app/production/database" },
                new SecretsManagerKnownSecretsOptions());
            sut.Load();

            Assert.That(sut.Get("my-app:production:database:Key"), Is.EqualTo("value"));
        }

        [Test]
        public void JSON_secret_without_name_prefix_uses_only_json_path()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupBatchResponse(secretsManager, "/my-app/shared", "{\"ConnectionStrings\":{\"Default\":\"server=.;\"}}");

            var options = new SecretsManagerKnownSecretsOptions
            {
                KeyMapping = { PrefixJsonKeysWithSecretName = false }
            };
            var sut = new SecretsManagerKnownSecretsConfigurationProvider(
                secretsManager, new[] { "/my-app/shared" }, options);
            sut.Load();

            Assert.That(sut.Get("ConnectionStrings:Default"), Is.EqualTo("server=.;"));
        }

        [Test]
        public void Scalar_secret_with_path_name_uses_colon_separator()
        {
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupBatchResponse(secretsManager, "/my-app/production/api-key", "my-api-key");

            var sut = new SecretsManagerKnownSecretsConfigurationProvider(
                secretsManager,
                new[] { "/my-app/production/api-key" },
                new SecretsManagerKnownSecretsOptions());
            sut.Load();

            Assert.That(sut.Get("my-app:production:api-key"), Is.EqualTo("my-api-key"));
        }
    }

    [TestFixture]
    public class KeyMappingDiscoveryProviderTests
    {
        private static void SetupDiscovery(IAmazonSecretsManager secretsManager, string secretName, string secretArn)
        {
            Mock.Get(secretsManager)
                .Setup(s => s.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse
                {
                    SecretList = new List<SecretListEntry>
                    {
                        new SecretListEntry { ARN = secretArn, Name = secretName }
                    }
                });
        }

        private static void SetupBatchResponse(IAmazonSecretsManager secretsManager, string secretName, string secretArn, string secretValue)
        {
            Mock.Get(secretsManager)
                .Setup(s => s.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchGetSecretValueResponse
                {
                    SecretValues = new List<SecretValueEntry>
                    {
                        new SecretValueEntry { ARN = secretArn, Name = secretName, SecretString = secretValue }
                    },
                    Errors = new List<APIErrorType>()
                });
        }

        [Test]
        public void Discovery_JSON_secret_with_path_name_uses_colon_separator()
        {
            const string secretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test";
            const string secretName = "/my-app/production/database";
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupDiscovery(secretsManager, secretName, secretArn);
            SetupBatchResponse(secretsManager, secretName, secretArn, "{\"Key\":\"value\"}");

            var sut = new SecretsManagerDiscoveryConfigurationProvider(
                secretsManager, new SecretsManagerDiscoveryOptions { UseBatchFetch = true });
            sut.Load();

            Assert.That(sut.Get("my-app:production:database:Key"), Is.EqualTo("value"));
        }

        [Test]
        public void Discovery_scalar_secret_with_path_name_uses_colon_separator()
        {
            const string secretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test";
            const string secretName = "/my-app/production/api-key";
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupDiscovery(secretsManager, secretName, secretArn);
            SetupBatchResponse(secretsManager, secretName, secretArn, "my-api-key");

            var sut = new SecretsManagerDiscoveryConfigurationProvider(
                secretsManager, new SecretsManagerDiscoveryOptions { UseBatchFetch = true });
            sut.Load();

            Assert.That(sut.Get("my-app:production:api-key"), Is.EqualTo("my-api-key"));
        }

        [Test]
        public void Discovery_JSON_secret_without_name_prefix()
        {
            const string secretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test";
            const string secretName = "/my-app/shared";
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupDiscovery(secretsManager, secretName, secretArn);
            SetupBatchResponse(secretsManager, secretName, secretArn, "{\"Feature\":\"enabled\"}");

            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = true,
                KeyMapping = { PrefixJsonKeysWithSecretName = false }
            };
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(sut.Get("Feature"), Is.EqualTo("enabled"));
        }

        [Test]
        public void Discovery_JSON_secret_with_target_section()
        {
            const string secretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:test";
            const string secretName = "/my-app/production/database";
            var secretsManager = Mock.Of<IAmazonSecretsManager>();
            SetupDiscovery(secretsManager, secretName, secretArn);
            SetupBatchResponse(secretsManager, secretName, secretArn, "{\"ConnectionStrings\":{\"Database\":\"server=.;\"}}");

            var options = new SecretsManagerDiscoveryOptions
            {
                UseBatchFetch = true,
                KeyMapping =
                {
                    PrefixJsonKeysWithSecretName = false,
                    TargetSection = "App"
                }
            };
            var sut = new SecretsManagerDiscoveryConfigurationProvider(secretsManager, options);
            sut.Load();

            Assert.That(sut.Get("App:ConnectionStrings:Database"), Is.EqualTo("server=.;"));
        }
    }
}