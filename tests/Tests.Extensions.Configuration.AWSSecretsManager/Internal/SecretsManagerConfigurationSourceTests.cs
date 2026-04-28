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
        public void Build_with_direct_client_creates_provider(IAmazonSecretsManager client, IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerConfigurationSource(client, new SecretsManagerOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, CustomAutoData]
        public void Build_without_client_creates_provider(IConfigurationBuilder configBuilder)
        {
            var source = new SecretsManagerConfigurationSource(new SecretsManagerOptions());
            var provider = source.Build(configBuilder);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }
    }
}
