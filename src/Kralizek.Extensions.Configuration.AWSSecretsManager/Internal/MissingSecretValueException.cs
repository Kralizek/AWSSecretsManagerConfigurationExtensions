using System;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class MissingSecretValueException : Exception
    {
        public MissingSecretValueException(string errorMessage, string secretName, string secretArn, Exception exception) : base(errorMessage, exception)
        {
            this.SecretName = secretName;
            this.SecretArn = secretArn;
        }

        public string SecretArn { get; set; }

        public string SecretName { get; set; }
    }
}