using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Moq;
using Amazon;
using Kralizek.Extensions.Configuration.Internal;
using Amazon.Runtime;
using Kralizek.Extensions.Configuration;

namespace Tests
{
    [TestFixture]
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

        [Test, AutoMoqData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_region(RegionEndpoint region)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, region: region);

            configurationBuilder.Verify(m => m.Add(It.Is<SecretsManagerConfigurationSource>(s => s.Region == region)));
        }

        [Test, AutoMoqData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_credentials(AWSCredentials credentials)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, credentials);

            configurationBuilder.Verify(m => m.Add(It.IsAny<SecretsManagerConfigurationSource>()));
        }

        [Test, AutoMoqData]
        public void SecretsManagerConfigurationSource_can_be_added_via_convenience_method_with_credentials_and_region(AWSCredentials credentials, RegionEndpoint region)
        {
            configurationBuilder.Setup(m => m.Add(It.IsAny<IConfigurationSource>()));

            SecretsManagerExtensions.AddSecretsManager(configurationBuilder.Object, credentials, region);

            configurationBuilder.Verify(m => m.Add(It.Is<SecretsManagerConfigurationSource>(s => s.Region == region)));
        }
    }
}