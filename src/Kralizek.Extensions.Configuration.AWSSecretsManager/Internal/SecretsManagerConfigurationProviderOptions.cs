using System;
using System.Collections.Generic;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Kralizek.Extensions.Configuration.Internal
{
    public sealed class SecretsManagerOptions
    {
        /// <summary>
        /// A list of identifiers for the secrets that are to be retrieved.
        /// The secret ARN (full or partial) and secret name are supported.
        /// <remarks>
        /// See <see cref="GetSecretValueRequest.SecretId"/> for more info on supported values.
        /// </remarks>
        /// <example>
        /// <code>
        /// AcceptedSecretArns = new List&lt;string&gt;
        /// {
        ///     "MySecretFullARN-abcxyz",
        ///     "MySecretPartialARN",
        ///     "MySecretUniqueName"
        /// };
        /// </code>
        /// </example>
        /// </summary>
        public List<string> AcceptedSecretArns { get; set; } = new();
        
        /// <summary>
        /// A function that determines whether or not a given secret should be retrieved.
        /// </summary>
        /// <example>
        /// <code>SecretFilter = secret => secret.Name.Contains("IncludeMe");</code>
        /// </example>
        public Func<SecretListEntry, bool> SecretFilter { get; set; } = _ => true;

        /// <summary>
        /// A list of filters that get passed to the client to filter the listed secrets before returning them. 
        /// </summary>
        /// <example>
        /// <code>
        /// ListSecretsFilters = new List&lt;Filter&gt;
        /// {
        ///     new Filter
        ///     {
        ///         Key = FilterNameStringType.Name,
        ///         Values = new List&lt;string&gt; { "IncludeMe" }
        ///     }
        /// };
        /// </code>
        /// </example>
        public List<Filter> ListSecretsFilters { get; set; } = new();

        /// <summary>
        /// Defines a function that can be used to generate secret keys.
        /// </summary>
        /// <example>
        /// <code>
        /// KeyGenerator = (secret, key) => key.ToUpper();
        /// </code>
        /// </example>
        public Func<SecretListEntry, string, string> KeyGenerator { get; set; } = (secret, key) => key;
        
        /// <summary>
        /// Defines a function that can be used to customize the <see cref="GetSecretValueRequest"/> before it is sent.
        /// </summary>
        /// <example>
        /// <code>
        /// ConfigureSecretValueRequest = (request, context) => request.VersionStage = "AWSCURRENT";
        /// </code>
        /// </example>
        public Action<GetSecretValueRequest, SecretValueContext> ConfigureSecretValueRequest { get; set; } = (_, _) => { };
        
        /// <summary>
        /// The time that should be waited before refreshing the secrets.
        /// If null, secrets will not be refreshed.
        /// </summary>
        /// <example>
        /// <code>
        /// PollingInterval = TimeSpan.FromMinutes(15);
        /// </code>
        /// </example>
        public TimeSpan? PollingInterval { get; set; }        
    }
    
    public sealed class SecretsManagerConfigurationProviderOptions
    {
        public SecretsManagerConfigurationProviderOptions(AWSOptions awsOptions, SecretsManagerOptions secretsManagerOptions)
        {
            AWSOptions = awsOptions ?? throw new ArgumentNullException(nameof(awsOptions));
            SecretsManagerOptions = secretsManagerOptions ?? throw new ArgumentNullException(nameof(secretsManagerOptions));
        }

        public SecretsManagerConfigurationProviderOptions() : this(new AWSOptions(), new SecretsManagerOptions()) { }
        
        public AWSOptions AWSOptions { get; }

        public SecretsManagerOptions SecretsManagerOptions { get; }

        /// <summary>
        /// A function that can be used to configure the <see cref="AmazonSecretsManagerClient"/>
        /// that's injected into the client.
        /// </summary>
        /// <example>
        /// <code>
        /// ConfigureClient = config => config.Timeout = TimeSpan.FromSeconds(5);
        /// </code>
        /// </example>
        public Action<ClientConfig> ConfigureClient { get; set; } = _ => { };

        /// <summary>
        /// A function that can be used to provide a custom method to create a client.
        /// </summary>
        /// <example>
        /// <code>
        /// CreateClient = () => new MyCustomSecretsManagerClient();
        /// </code>
        /// </example>
        public Func<IAmazonSecretsManager>? CreateClient { get; set; }
    }
}