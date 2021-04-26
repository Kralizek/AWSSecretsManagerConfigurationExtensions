﻿using System.IO;
using System.Text;
using AutoFixture.NUnit3;
using AutoFixture;
using AutoFixture.AutoMoq;
using Kralizek.Extensions.Configuration.Internal;
using Amazon.SecretsManager.Model;
using System.Collections.Generic;

namespace Tests
{
    public class CustomAutoDataAttribute : AutoDataAttribute
    {
        public CustomAutoDataAttribute() : base(CreateFixture) { }

        private static IFixture CreateFixture()
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