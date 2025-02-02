using System;

using Amazon.Runtime;
using Amazon.SecretsManager;

using Kralizek.Extensions.Configuration.Internal;

using Microsoft.Extensions.Configuration;

using Moq;

using NUnit.Framework;

namespace Tests.Internal
{
    [TestFixture]
    [TestOf(typeof(SecretsManagerConfigurationSource))]
    public class SecretsManagerConfigurationSourceTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1", EnvironmentVariableTarget.Process);
        }

        [Test, CustomAutoData]
        public void Build_can_create_a_IConfigurationProvider(IConfigurationBuilder configurationBuilder)
        {
            var sut = new SecretsManagerConfigurationSource();

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_can_create_a_IConfigurationProvider_with_credentials(AWSCredentials credentials, IConfigurationBuilder configurationBuilder)
        {
            var sut = new SecretsManagerConfigurationSource(credentials);

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_can_create_a_IConfigurationProvider_with_options(SecretsManagerConfigurationProviderOptions options, IConfigurationBuilder configurationBuilder)
        {
            var sut = new SecretsManagerConfigurationSource(options: options);

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_invokes_config_client_method(IConfigurationBuilder configurationBuilder, Action<AmazonSecretsManagerConfig> secretsManagerConfiguration)
        {
            var options = new SecretsManagerConfigurationProviderOptions
            {
                ConfigureSecretsManagerConfig = secretsManagerConfiguration
            };

            var sut = new SecretsManagerConfigurationSource(options: options);

            var provider = sut.Build(configurationBuilder);

            Mock.Get(secretsManagerConfiguration).Verify(p => p(It.Is<AmazonSecretsManagerConfig>(c => c != null)), Times.Once());
        }

        [Test, CustomAutoData]
        public void Build_uses_given_client_factory_method(IConfigurationBuilder configurationBuilder, SecretsManagerConfigurationProviderOptions options, Func<IAmazonSecretsManager> clientFactory)
        {
            options.CreateClient = clientFactory;

            var sut = new SecretsManagerConfigurationSource(options: options);

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Mock.Get(clientFactory).Verify(p => p());
        }
    }
}