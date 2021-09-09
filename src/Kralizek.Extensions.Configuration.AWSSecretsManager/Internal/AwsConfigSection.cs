namespace Kralizek.Extensions.Configuration.Internal
{
    /// <summary>
    /// An object intended to bind an <see cref="IConfigurationSection"/> to.
    /// </summary>
    internal class AwsConfigSection
    {
        public const string DefaultConfigSectionName = "AWS";
        
        /// <summary>
        /// The string representation of an AWS Region.
        /// </summary>
        public string Region { get; set; }
        
        /// <summary>
        /// The name of the credentials profile to be used by the Secrets Manager client.
        /// </summary>
        public string Profile { get; set; }
        
        /// <summary>
        /// The location of the file containing the credentials profiles.
        /// </summary>
        public string ProfilesLocation { get; set; }
    }
}