using System;

using Amazon.SecretsManager;

using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;

using Microsoft.Extensions.Configuration;

using Moq;

using NUnit.Framework;

namespace Tests.Internal
{
    [TestFixture]
    public class SecretsManagerConfigurationSourceTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1", EnvironmentVariableTarget.Process);
        }

        [Test, CustomAutoData]
        public void Build_with_direct_client_creates_discovery_provider(IAmazonSecretsManager client, IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerDiscoveryConfigurationSource(client, new SecretsManagerDiscoveryOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerDiscoveryConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_without_client_creates_discovery_provider(IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerDiscoveryConfigurationSource(new SecretsManagerDiscoveryOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerDiscoveryConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_with_direct_client_creates_known_secret_provider(IAmazonSecretsManager client, IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerKnownSecretConfigurationSource(client, "my-secret", new SecretsManagerKnownSecretOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerKnownSecretConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_without_client_creates_known_secret_provider(IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerKnownSecretConfigurationSource("my-secret", new SecretsManagerKnownSecretOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerKnownSecretConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_with_direct_client_creates_known_secrets_provider(IAmazonSecretsManager client, IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerKnownSecretsConfigurationSource(client, new[] { "my-secret" }, new SecretsManagerKnownSecretsOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerKnownSecretsConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_without_client_creates_known_secrets_provider(IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerKnownSecretsConfigurationSource(new[] { "my-secret" }, new SecretsManagerKnownSecretsOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerKnownSecretsConfigurationProvider>());
        }
    }
}