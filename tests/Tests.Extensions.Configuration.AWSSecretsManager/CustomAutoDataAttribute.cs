using System.IO;
using System.Text;
using AutoFixture.NUnit3;
using AutoFixture;
using AutoFixture.AutoMoq;
using Kralizek.Extensions.Configuration.Internal;

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

            return fixture;
        }
    }
}