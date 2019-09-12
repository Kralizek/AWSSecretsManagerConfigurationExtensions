using System;

namespace Kralizek.Extensions.Configuration.Internal
{
    public class MissingSecretValueException : Exception
    {
        public MissingSecretValueException(string errorMessage, Exception exception) : base(errorMessage, exception)
        {
            
        }
    }
}