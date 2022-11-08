using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SecretsManager.Model;
using Kralizek.Extensions.Configuration.Internal;
// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration
{
    public static class SecretsManagerExtensions
    {
        public static IConfigurationBuilder AddSecretsManager(this IConfigurationBuilder configurationBuilder, Action<ISecretsManagerConfigurationBuilder>? configure = null)
        {
            var builder = new SecretsManagerConfigurationBuilder();
            
            configure?.Invoke(builder);

            var options = builder.BuildConfiguration();

            var source = new SecretsManagerConfigurationSource(options);

            configurationBuilder.Add(source);

            return configurationBuilder;
        }

        public static SecretsManagerOptions GetSecretsManagerOptions(this IConfigurationSection section)
        {
            // TODO: Implement parsing
            return new SecretsManagerOptions();
        }

        public static ISecretsManagerConfigurationBuilder UseConfiguration(this ISecretsManagerConfigurationBuilder configurationBuilder, SecretsManagerOptions options)
        {
            configurationBuilder.OptionsFactory = () => options;
            
            return configurationBuilder;
        }

        public static ISecretsManagerConfigurationBuilder UseAWSOptions(this ISecretsManagerConfigurationBuilder configurationBuilder, AWSOptions options)
        {
            configurationBuilder.AWSOptionsFactory = () => options;
            
            return configurationBuilder;
        }
    }

    public interface ISecretsManagerConfigurationBuilder
    {
        Func<SecretsManagerOptions> OptionsFactory { get; set; }
        
        Func<AWSOptions> AWSOptionsFactory { get; set; }
        
        void Configure(Action<SecretsManagerOptions> configure);

        void ConfigureAWS(Action<AWSOptions> configure);

    }

    internal sealed class SecretsManagerConfigurationBuilder : ISecretsManagerConfigurationBuilder
    {
        private readonly IList<Action<SecretsManagerOptions>> _optionsActions = new List<Action<SecretsManagerOptions>>();
        private readonly IList<Action<AWSOptions>> _awsOptionsactions = new List<Action<AWSOptions>>();

        public SecretsManagerConfigurationProviderOptions BuildConfiguration()
        {
            var awsOptions = AWSOptionsFactory();
            var secretManagerOptions = OptionsFactory();

            var options = new SecretsManagerConfigurationProviderOptions(awsOptions, secretManagerOptions);
            
            foreach (var action in _optionsActions)
            {
                action(options.SecretsManagerOptions);
            }

            foreach (var action in _awsOptionsactions)
            {
                action(options.AWSOptions);
            }

            return options;
        }

        public Func<SecretsManagerOptions> OptionsFactory { get; set; } = () => new SecretsManagerOptions();

        public Func<AWSOptions> AWSOptionsFactory { get; set; } = () => new AWSOptions();

        public void Configure(Action<SecretsManagerOptions> configure)
        {
            _optionsActions.Add(configure);
        }

        public void ConfigureAWS(Action<AWSOptions> configure)
        {
            _awsOptionsactions.Add(configure);
        }
    }
}