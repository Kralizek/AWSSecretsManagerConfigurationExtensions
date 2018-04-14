using System.IO;
using System.Text;
using AutoFixture.NUnit3;
using AutoFixture;
using AutoFixture.AutoMoq;

namespace Tests
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute() : base(CreateFixture) { }

        private static IFixture CreateFixture()
        {
            IFixture fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            return fixture;
        }
    }
}