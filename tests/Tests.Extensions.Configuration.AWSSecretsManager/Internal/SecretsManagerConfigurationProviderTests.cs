using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AutoFixture;
using Kralizek.Extensions.Configuration;
using Kralizek.Extensions.Configuration.Internal;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Tests.Types;

namespace Tests.Internal
{
    [TestFixture]
    public class SecretsManagerConfigurationProviderTests
    {
        private IFixture fixture;
        private Mock<IAmazonSecretsManager> mockSecretsManager;

        [SetUp]
        public void Initialize()
        {
            fixture = new Fixture();

            fixture.Customize<MemoryStream>(c =>
            {
                return c.FromFactory((string str) =>
                {
                    var bytes = Encoding.UTF8.GetBytes(str);
                    return new MemoryStream(bytes);
                }).OmitAutoProperties();
            });

            mockSecretsManager = new Mock<IAmazonSecretsManager>();
        }

        private SecretsManagerConfigurationProvider CreateSystemUnderTest()
        {
            return new SecretsManagerConfigurationProvider(mockSecretsManager.Object);
        }

        [Test, AutoMoqData]
        public void Simple_values_in_string_can_be_handled(SecretListEntry testEntry)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString)
                                                .Without(p => p.SecretBinary)
                                                .Create();

            mockSecretsManager.Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            mockSecretsManager.Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            var sut = CreateSystemUnderTest();

            sut.Load();

            Assert.That(sut.Get(testEntry.Name), Is.EqualTo(getSecretValueResponse.SecretString));
        }

        [Test, AutoMoqData]
        public void Complex_JSON_objects_in_string_can_be_handled(SecretListEntry testEntry, RootObject test)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretString, JsonConvert.SerializeObject(test))
                                                .Without(p => p.SecretBinary)
                                                .Create();

            mockSecretsManager.Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            mockSecretsManager.Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            var sut = CreateSystemUnderTest();

            sut.Load();

            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Property)), Is.EqualTo(test.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Property)), Is.EqualTo(test.Mid.Property));
            Assert.That(sut.Get(testEntry.Name, nameof(RootObject.Mid), nameof(MidLevel.Leaf), nameof(Leaf.Property)), Is.EqualTo(test.Mid.Leaf.Property));
        }

        [Test, AutoMoqData]
        public void Values_in_binary_are_ignored(SecretListEntry testEntry)
        {
            var secretListResponse = fixture.Build<ListSecretsResponse>()
                                            .With(p => p.SecretList, new List<SecretListEntry> { testEntry })
                                            .With(p => p.NextToken, null)
                                            .Create();

            var getSecretValueResponse = fixture.Build<GetSecretValueResponse>()
                                                .With(p => p.SecretBinary)
                                                .Without(p => p.SecretString)
                                                .Create();

            mockSecretsManager.Setup(p => p.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(secretListResponse);

            mockSecretsManager.Setup(p => p.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(getSecretValueResponse);

            var sut = CreateSystemUnderTest();

            sut.Load();

            Assert.That(sut.HasKey(testEntry.Name), Is.False);
        }
    }
}