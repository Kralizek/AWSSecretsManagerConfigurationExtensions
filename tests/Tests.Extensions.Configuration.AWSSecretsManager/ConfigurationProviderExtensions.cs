using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Tests
{
    public static class ConfigurationProviderExtensions
    {
        public static string Get(this IConfigurationProvider provider, params string[] pathSegments)
        {
            var key = ConfigurationPath.Combine(pathSegments);

            if (provider.TryGet(key, out var value))
            {
                return value;
            }

            return null;
        }

        public static bool HasKey(this IConfigurationProvider provider, params string[] pathSegments)
        {
            var key = ConfigurationPath.Combine(pathSegments);

            return provider.TryGet(key, out var _);
        }
    }

    [TestFixture]
    [TestOf(typeof(ConfigurationProviderExtensions))]
    public class ConfigurationProviderExtensionsTests
    {
        [Test, AutoMoqData]
        public void Added_keys_are_found(ConfigurationProvider provider, string key, string value)
        {
            provider.Set(key, value);

            Assert.That(ConfigurationProviderExtensions.HasKey(provider, key), Is.True);
        }

        [Test, AutoMoqData]
        public void Added_nested_keys_are_found(ConfigurationProvider provider, string firstKey, string secondKey, string value)
        {
            provider.Set($"{firstKey}{ConfigurationPath.KeyDelimiter}{secondKey}", value);

            Assert.That(ConfigurationProviderExtensions.HasKey(provider, firstKey, secondKey), Is.True);
        }

        [Test, AutoMoqData]
        public void Non_added_keys_are_not_found(ConfigurationProvider provider, string key)
        {
            Assert.That(ConfigurationProviderExtensions.HasKey(provider, key), Is.False);
        }

        [Test, AutoMoqData]
        public void Values_can_be_retrieved(ConfigurationProvider provider, string key, string value)
        {
            provider.Set(key, value);

            Assert.That(ConfigurationProviderExtensions.Get(provider, key), Is.EqualTo(value));
        }

        [Test, AutoMoqData]
        public void Values_of_nested_keys_can_be_retrieved(ConfigurationProvider provider, string firstKey, string secondKey, string value)
        {
            provider.Set($"{firstKey}{ConfigurationPath.KeyDelimiter}{secondKey}", value);

            Assert.That(ConfigurationProviderExtensions.Get(provider, firstKey, secondKey), Is.EqualTo(value));
        }

        [Test, AutoMoqData]
        public void Non_added_keys_return_null(ConfigurationProvider provider, string key)
        {
            Assert.That(ConfigurationProviderExtensions.Get(provider, key), Is.Null);
        }
    }
}