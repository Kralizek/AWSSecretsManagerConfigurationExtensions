using System;
using Microsoft.Extensions.Configuration;

namespace Kralizek.Extensions.Configuration.Internal
{
    internal sealed class SecretsManagerConfigurationSource : IConfigurationSource
    {
        private readonly SecretsManagerConfigurationProviderOptions _options;
        
        public SecretsManagerConfigurationSource(SecretsManagerConfigurationProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SecretsManagerConfigurationProvider(_options);
        }
    }
}
