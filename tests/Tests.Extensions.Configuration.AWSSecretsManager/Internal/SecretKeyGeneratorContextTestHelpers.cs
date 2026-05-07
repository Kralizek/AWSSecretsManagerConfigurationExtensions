using System.Collections.Generic;
using System.Threading;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using Kralizek.Extensions.Configuration;

using Moq;

using NUnit.Framework;

namespace Tests.Internal
{
    internal sealed class CapturingKeyGenerator
    {
        private readonly List<SecretKeyGeneratorContext> _contexts = [];

        public IReadOnlyList<SecretKeyGeneratorContext> Contexts => _contexts;

        public string Generate(SecretKeyGeneratorContext context)
        {
            _contexts.Add(context);
            return context.DefaultKey;
        }

        public SecretKeyGeneratorContext SingleContext
        {
            get
            {
                Assert.That(_contexts, Has.Count.EqualTo(1));
                return _contexts[0];
            }
        }
    }

    internal static class SecretKeyGeneratorContextAssertions
    {
        public static void AssertJsonContext(
            SecretKeyGeneratorContext context,
            string expectedSecretId,
            string expectedSecretName,
            string expectedSecretArn,
            string expectedKey,
            string expectedJsonPath)
        {
            AssertContextCore(context, expectedSecretId, expectedSecretName, expectedSecretArn, expectedKey);
            Assert.That(context.JsonPath, Is.EqualTo(expectedJsonPath));
            Assert.That(context.HasJsonPath, Is.True);
        }

        public static void AssertScalarContext(
            SecretKeyGeneratorContext context,
            string expectedSecretId,
            string expectedSecretName,
            string expectedSecretArn,
            string expectedKey)
        {
            AssertContextCore(context, expectedSecretId, expectedSecretName, expectedSecretArn, expectedKey);
            Assert.That(context.JsonPath, Is.Null);
            Assert.That(context.HasJsonPath, Is.False);
        }

        private static void AssertContextCore(
            SecretKeyGeneratorContext context,
            string expectedSecretId,
            string expectedSecretName,
            string expectedSecretArn,
            string expectedKey)
        {
            Assert.That(context.SecretId, Is.EqualTo(expectedSecretId));
            Assert.That(context.SecretName, Is.EqualTo(expectedSecretName));
            Assert.That(context.SecretArn, Is.EqualTo(expectedSecretArn));
            Assert.That(context.RawKey, Is.EqualTo(expectedKey));
            Assert.That(context.DefaultKey, Is.EqualTo(expectedKey));
        }
    }

    internal static class SecretKeyGeneratorContextTestData
    {
        public const string ConfiguredSecretId = "configured-secret-id";
        public const string SecretArn = "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-AbCdEf";
        public const string SecretName = "my-secret";
        public const string JsonSecretValue = "{\"Property\":\"value\"}";
        public const string ScalarSecretValue = "value";
        public const string JsonPath = "Property";
        public const string JsonGeneratedKey = "my-secret:Property";

        public static void SetupDiscoverySecretList(IAmazonSecretsManager secretsManager)
        {
            Mock.Get(secretsManager)
                .Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = new List<SecretListEntry> { new SecretListEntry { ARN = SecretArn, Name = SecretName } } });
        }

        public static void SetupGetSecretValueAny(IAmazonSecretsManager secretsManager, string secretValue)
        {
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateGetSecretValueResponse(secretValue));
        }

        public static void SetupGetSecretValueForConfiguredSecretId(IAmazonSecretsManager secretsManager, string configuredSecretId, string secretValue)
        {
            Mock.Get(secretsManager)
                .Setup(p => p.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == configuredSecretId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateGetSecretValueResponse(secretValue));
        }

        public static void SetupBatchGetSecretValueAny(IAmazonSecretsManager secretsManager, string secretValue)
        {
            Mock.Get(secretsManager)
                .Setup(p => p.BatchGetSecretValueAsync(It.IsAny<BatchGetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchGetSecretValueResponse
                {
                    SecretValues = new List<SecretValueEntry> { new SecretValueEntry { ARN = SecretArn, Name = SecretName, SecretString = secretValue } },
                    Errors = new List<APIErrorType>()
                });
        }

        private static GetSecretValueResponse CreateGetSecretValueResponse(string secretValue)
        {
            return new GetSecretValueResponse
            {
                ARN = SecretArn,
                Name = SecretName,
                SecretString = secretValue
            };
        }
    }
}