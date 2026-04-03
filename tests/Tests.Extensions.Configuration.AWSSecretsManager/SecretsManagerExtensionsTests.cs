using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Kralizek.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [TestOf(typeof(SecretsManagerExtensions))]
    public class SecretsManagerExtensionsTests
    {
        private Mock<IConfigurationBuilder> _builder;

        [SetUp]
        public void Initialize() => _builder = new Mock<IConfigurationBuilder>();

        [Test]
        public void AddSecretsManager_no_args_adds_source()
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object);
            _builder.Verify(b => b.Add(It.IsAny<IConfigurationSource>()));
        }

        [Test]
        public void AddSecretsManager_with_configure_invokes_delegate()
        {
            var invoked = false;
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, opts => { invoked = true; });
            _builder.Verify(b => b.Add(It.IsAny<IConfigurationSource>()));
            Assert.That(invoked, Is.True);
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_AWSOptions_adds_source(AWSOptions awsOptions)
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, awsOptions);
            _builder.Verify(b => b.Add(It.IsAny<IConfigurationSource>()));
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_AWSOptions_configure_invokes_delegate(AWSOptions awsOptions)
        {
            var invoked = false;
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, awsOptions, opts => { invoked = true; });
            _builder.Verify(b => b.Add(It.IsAny<IConfigurationSource>()));
            Assert.That(invoked, Is.True);
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_client_adds_source(IAmazonSecretsManager client)
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, client);
            _builder.Verify(b => b.Add(It.IsAny<IConfigurationSource>()));
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_client_configure_invokes_delegate(IAmazonSecretsManager client)
        {
            var invoked = false;
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, client, opts => { invoked = true; });
            _builder.Verify(b => b.Add(It.IsAny<IConfigurationSource>()));
            Assert.That(invoked, Is.True);
        }
    }
}
