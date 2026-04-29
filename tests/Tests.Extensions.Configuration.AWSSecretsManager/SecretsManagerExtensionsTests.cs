using System;
using System.Collections.Generic;

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

        [SetUp]
        public void Initialize()
        {
            _builder = new Mock<IConfigurationBuilder>();
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()));
        }

        #region AddSecretsManagerDiscovery

        [Test]
        public void AddSecretsManagerDiscovery_no_args_adds_discovery_source()
        {
            SecretsManagerDiscoveryConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerDiscoveryConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerDiscovery(_builder.Object);

            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void AddSecretsManagerDiscovery_with_configure_applies_options()
        {
            SecretsManagerDiscoveryConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerDiscoveryConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerDiscovery(_builder.Object,
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(1));

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerDiscovery_with_AWSOptions_wires_awsOptions(AWSOptions awsOptions)
        {
            SecretsManagerDiscoveryConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerDiscoveryConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerDiscovery(_builder.Object, awsOptions);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.AwsOptions, Is.SameAs(awsOptions));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerDiscovery_with_AWSOptions_configure_applies_options_and_wires_awsOptions(AWSOptions awsOptions)
        {
            SecretsManagerDiscoveryConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerDiscoveryConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerDiscovery(_builder.Object, awsOptions,
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(2));

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.AwsOptions, Is.SameAs(awsOptions));
            Assert.That(captured!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerDiscovery_with_client_wires_client(IAmazonSecretsManager client)
        {
            SecretsManagerDiscoveryConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerDiscoveryConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerDiscovery(_builder.Object, client);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Client, Is.SameAs(client));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerDiscovery_with_client_configure_applies_options_and_wires_client(IAmazonSecretsManager client)
        {
            SecretsManagerDiscoveryConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerDiscoveryConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerDiscovery(_builder.Object, client,
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(3));

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Client, Is.SameAs(client));
            Assert.That(captured!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(3)));
        }

        [Test]
        public void AddSecretsManagerDiscovery_null_builder_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SecretsManagerExtensions.AddSecretsManagerDiscovery(null!));
        }

        #endregion

        #region AddSecretsManagerKnownSecret

        [Test]
        public void AddSecretsManagerKnownSecret_adds_known_secret_source()
        {
            SecretsManagerKnownSecretConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, "my-secret");

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.SecretId, Is.EqualTo("my-secret"));
        }

        [Test]
        public void AddSecretsManagerKnownSecret_with_configure_applies_options()
        {
            SecretsManagerKnownSecretConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, "my-secret",
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(1));

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerKnownSecret_with_AWSOptions_wires_awsOptions(AWSOptions awsOptions)
        {
            SecretsManagerKnownSecretConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, awsOptions, "my-secret");

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.AwsOptions, Is.SameAs(awsOptions));
            Assert.That(captured!.SecretId, Is.EqualTo("my-secret"));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerKnownSecret_with_client_wires_client(IAmazonSecretsManager client)
        {
            SecretsManagerKnownSecretConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, client, "my-secret");

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Client, Is.SameAs(client));
            Assert.That(captured!.SecretId, Is.EqualTo("my-secret"));
        }

        [Test]
        public void AddSecretsManagerKnownSecret_null_builder_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecret(null!, "my-secret"));
        }

        [Test]
        public void AddSecretsManagerKnownSecret_null_secretId_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, (string)null!));
        }

        [Test]
        public void AddSecretsManagerKnownSecret_empty_secretId_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, ""));
        }

        [Test]
        public void AddSecretsManagerKnownSecret_whitespace_secretId_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecret(_builder.Object, "   "));
        }

        #endregion

        #region AddSecretsManagerKnownSecrets

        [Test]
        public void AddSecretsManagerKnownSecrets_adds_known_secrets_source()
        {
            SecretsManagerKnownSecretsConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretsConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object,
                new[] { "shared/config", "app/config" });

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.SecretIds, Is.EqualTo(new[] { "shared/config", "app/config" }));
        }

        [Test]
        public void AddSecretsManagerKnownSecrets_with_configure_applies_options()
        {
            SecretsManagerKnownSecretsConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretsConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object,
                new[] { "shared/config" },
                opts => opts.ReloadInterval = TimeSpan.FromMinutes(1));

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Options.ReloadInterval, Is.EqualTo(TimeSpan.FromMinutes(1)));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerKnownSecrets_with_AWSOptions_wires_awsOptions(AWSOptions awsOptions)
        {
            SecretsManagerKnownSecretsConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretsConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object, awsOptions,
                new[] { "shared/config" });

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.AwsOptions, Is.SameAs(awsOptions));
        }

        [Test, CustomAutoData]
        public void AddSecretsManagerKnownSecrets_with_client_wires_client(IAmazonSecretsManager client)
        {
            SecretsManagerKnownSecretsConfigurationSource? captured = null;
            _builder.Setup(b => b.Add(It.IsAny<IConfigurationSource>()))
                .Callback<IConfigurationSource>(s => captured = s as SecretsManagerKnownSecretsConfigurationSource);

            SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object, client,
                new[] { "shared/config" });

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Client, Is.SameAs(client));
        }

        [Test]
        public void AddSecretsManagerKnownSecrets_null_builder_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecrets(null!, new[] { "x" }));
        }

        [Test]
        public void AddSecretsManagerKnownSecrets_null_secretIds_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object, (IEnumerable<string>)null!));
        }

        [Test]
        public void AddSecretsManagerKnownSecrets_empty_secretIds_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object, Array.Empty<string>()));
        }

        [Test]
        public void AddSecretsManagerKnownSecrets_secretIds_with_null_element_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object, new string[] { null! }));
        }

        [Test]
        public void AddSecretsManagerKnownSecrets_secretIds_with_whitespace_element_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SecretsManagerExtensions.AddSecretsManagerKnownSecrets(_builder.Object, new[] { "  " }));
        }

        #endregion

        #region AddSecretsManager removed

        [Test]
        [Description("AddSecretsManager(IConfigurationBuilder) should no longer exist")]
        public void AddSecretsManager_no_longer_exists()
        {
            var method = typeof(SecretsManagerExtensions).GetMethod("AddSecretsManager");
            Assert.That(method, Is.Null, "AddSecretsManager should have been removed from the public API");
        }

        #endregion
    }
}