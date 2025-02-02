using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Amazon.SecretsManager.Model;

using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;

using Kralizek.Extensions.Configuration.Internal;

namespace Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CustomAutoDataAttribute : AutoDataAttribute
    {
        public CustomAutoDataAttribute() : base(FixtureHelpers.CreateFixture)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CustomInlineAutoDataAttribute : InlineAutoDataAttribute
    {
        public CustomInlineAutoDataAttribute(params object[] args) : base(FixtureHelpers.CreateFixture, args)
        {
        }
    }

    public static class FixtureHelpers
    {
        public static IFixture CreateFixture()
        {
            IFixture fixture = new Fixture();

            fixture.Customize(new AutoMoqCustomization
            {
                GenerateDelegates = true
            });

            fixture.Customize<SecretsManagerConfigurationProviderOptions>(o => o.OmitAutoProperties());

            fixture.Customize<MemoryStream>(c =>
            {
                return c.FromFactory((string str) =>
                {
                    var bytes = Encoding.UTF8.GetBytes(str);
                    return new MemoryStream(bytes);
                }).OmitAutoProperties();
            });

            fixture.Customize<ListSecretsResponse>(o => o
                        .With(p => p.SecretList, (SecretListEntry entry) => new List<SecretListEntry> { entry })
                        .Without(p => p.NextToken));

            fixture.Customize<GetSecretValueResponse>(o => o
                        .With(p => p.SecretString)
                        .Without(p => p.SecretBinary));

            return fixture;
        }
    }
}