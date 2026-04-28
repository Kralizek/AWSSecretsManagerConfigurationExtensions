using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    [TestOf(typeof(SecretsManagerExtensions))]
    public class SecretsManagerExtensionsTests
    {
        private Mock<IConfigurationBuilder> _builder = null!;
        private SecretsManagerConfigurationSource? _capturedSource;

        [SetUp]
        public void Initialize()
        {
            _capturedSource = null;
            _builder = new Mock<IConfigurationBuilder>();
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => _capturedSource = s as SecretsManagerConfigurationSource);
        }

        [Test]
        public void AddSecretsManager_no_args_adds_SecretsManagerConfigurationSource()
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object);
            Assert.That(_capturedSource, Is.Not.Null);
        }

        [Test]
        public void AddSecretsManager_with_configure_applies_options_to_source()
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object,
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(1));

            Assert.That(_capturedSource, Is.Not.Null);
            Assert.That(_capturedSource!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_AWSOptions_wires_awsOptions_into_source(AWSOptions awsOptions)
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, awsOptions);

            Assert.That(_capturedSource, Is.Not.Null);
            Assert.That(_capturedSource!.AwsOptions, Is.SameAs(awsOptions));
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_AWSOptions_configure_applies_options_and_wires_awsOptions(AWSOptions awsOptions)
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, awsOptions,
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(2));

            Assert.That(_capturedSource, Is.Not.Null);
            Assert.That(_capturedSource!.AwsOptions, Is.SameAs(awsOptions));
            Assert.That(_capturedSource!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_client_wires_client_into_source(IAmazonSecretsManager client)
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, client);

            Assert.That(_capturedSource, Is.Not.Null);
            Assert.That(_capturedSource!.Client, Is.SameAs(client));
        }

        [Test, CustomAutoData]
        public void AddSecretsManager_with_client_configure_applies_options_and_wires_client(IAmazonSecretsManager client)
        {
            SecretsManagerExtensions.AddSecretsManager(_builder.Object, client,
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(3));

            Assert.That(_capturedSource, Is.Not.Null);
            Assert.That(_capturedSource!.Client, Is.SameAs(client));
            Assert.That(_capturedSource!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(3)));
        }
    }
}
