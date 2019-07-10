using Amazon.Runtime;
using Amazon.SecretsManager;
using Kralizek.Extensions.Configuration.Internal;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Tests.Internal
{
    [TestFixture]
    public class SecretsManagerConfigurationSourceTests
    {
        [Test, AutoMoqData]
        public void Build_can_create_a_IConfigurationProvider(IConfigurationBuilder configurationBuilder)
        {
            var sut = new SecretsManagerConfigurationSource();

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, AutoMoqData]
        public void Build_can_create_a_IConfigurationProvider_with_credentials(AWSCredentials credentials, IConfigurationBuilder configurationBuilder)
        {
            var sut = new SecretsManagerConfigurationSource(credentials);

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, AutoMoqData]
        public void Build_can_create_a_IConfigurationProvider_with_options(SecretsManagerConfigurationProviderOptions options, IConfigurationBuilder configurationBuilder)
        {
            var sut = new SecretsManagerConfigurationSource(options: options);

            var provider = sut.Build(configurationBuilder);

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<SecretsManagerConfigurationProvider>());
        }

        [Test, AutoMoqData]
        public void Build_invokes_config_client_method(IConfigurationBuilder configurationBuilder)
        {
            bool configInvoked = false;
            AmazonSecretsManagerConfig usedConfig = null;
            var sut = new SecretsManagerConfigurationSource(options: new SecretsManagerConfigurationProviderOptions()
            {
                ConfigureSecretsManagerConfig = c =>
                {
                    usedConfig = c;
                    configInvoked = true;
                }
            });
            

            var provider = sut.Build(configurationBuilder);

            Assert.That(configInvoked, Is.True);
            Assert.That(usedConfig, Is.Not.Null);
            
        }


    }
}