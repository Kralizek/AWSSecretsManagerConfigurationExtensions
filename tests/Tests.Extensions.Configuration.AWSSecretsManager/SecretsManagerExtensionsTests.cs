using System;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Moq;
using Amazon;
using Kralizek.Extensions.Configuration.Internal;
using Amazon.Runtime;

namespace Tests
{
    [TestFixture]
    [TestOf(typeof(SecretsManagerExtensions))]
    public class SecretsManagerExtensionsTests
    {
        private Mock<IConfigurationBuilder> configurationBuilder;

        [SetUp]
        public void Initialize()
        {
            configurationBuilder = new Mock<IConfigurationBuilder>();
        }

        [Test]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_no_parameters()
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object);

            configurationBuilder.Verify(m => m.Add(It.IsAny<SecretsManagerConfigurationSource>()));
        }

        [Test, CustomAutoData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_region(RegionEndpoint region)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, region: region);

            configurationBuilder.Verify(m => m.Add(It.Is<SecretsManagerConfigurationSource>(s => s.Region == region)));
        }

        [Test, CustomAutoData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_optionConfigurator(Action<SecretsManagerConfigurationProviderOptions> optionConfigurator)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, configurator: optionConfigurator);

            configurationBuilder.Verify(m => m.Add(It.IsAny<SecretsManagerConfigurationSource>()));

            Mock.Get(optionConfigurator).Verify(p => p(It.IsAny<SecretsManagerConfigurationProviderOptions>()), Times.Once);
        }

        [Test, CustomAutoData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_credentials(AWSCredentials credentials)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, credentials);

            configurationBuilder.Verify(m => m.Add(It.IsAny<SecretsManagerConfigurationSource>()));
        }

        [Test, CustomAutoData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_credentials_and_region(AWSCredentials credentials, RegionEndpoint region)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, credentials, region);

            configurationBuilder.Verify(m => m.Add(It.Is<SecretsManagerConfigurationSource>(s => s.Region == region)));
        }

    }
}