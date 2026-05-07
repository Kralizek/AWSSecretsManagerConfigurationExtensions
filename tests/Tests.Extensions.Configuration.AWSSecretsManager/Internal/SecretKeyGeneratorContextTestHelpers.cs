using System.Collections.Generic;

using Kralizek.Extensions.Configuration;

using NUnit.Framework;

namespace Tests.Internal
{
    internal sealed class CapturingKeyGenerator
    {
        private readonly List<SecretKeyGeneratorContext> _contexts = [];

        public IReadOnlyList<SecretKeyGeneratorContext> Contexts => _contexts;

        public string Generate(SecretKeyGeneratorContext context)
        {
            _contexts.Add(context);
            return context.DefaultKey;
        }

        public SecretKeyGeneratorContext SingleContext
        {
            get
            {
                Assert.That(_contexts, Has.Count.EqualTo(1));
                return _contexts[0];
            }
        }
    }

    internal static class SecretKeyGeneratorContextAssertions
    {
        public static void AssertJsonContext(
            SecretKeyGeneratorContext context,
            string expectedSecretId,
            string expectedSecretName,
            string expectedSecretArn,
            string expectedKey,
            string expectedJsonPath)
        {
            Assert.That(context.SecretId, Is.EqualTo(expectedSecretId));
            Assert.That(context.SecretName, Is.EqualTo(expectedSecretName));
            Assert.That(context.SecretArn, Is.EqualTo(expectedSecretArn));
            Assert.That(context.RawKey, Is.EqualTo(expectedKey));
            Assert.That(context.DefaultKey, Is.EqualTo(expectedKey));
            Assert.That(context.JsonPath, Is.EqualTo(expectedJsonPath));
            Assert.That(context.HasJsonPath, Is.True);
        }

        public static void AssertScalarContext(
            SecretKeyGeneratorContext context,
            string expectedSecretId,
            string expectedSecretName,
            string expectedSecretArn,
            string expectedKey)
        {
            Assert.That(context.SecretId, Is.EqualTo(expectedSecretId));
            Assert.That(context.SecretName, Is.EqualTo(expectedSecretName));
            Assert.That(context.SecretArn, Is.EqualTo(expectedSecretArn));
            Assert.That(context.RawKey, Is.EqualTo(expectedKey));
            Assert.That(context.DefaultKey, Is.EqualTo(expectedKey));
            Assert.That(context.JsonPath, Is.Null);
            Assert.That(context.HasJsonPath, Is.False);
        }
    }
}
