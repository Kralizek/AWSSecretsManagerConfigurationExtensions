using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration
{
    public static class SecretsManagerLogEvents
    {
        public static readonly EventId LoadStarted = new(1000, "LoadStarted");
        public static readonly EventId LoadCompleted = new(1001, "LoadCompleted");
        public static readonly EventId SecretLoaded = new(1002, "SecretLoaded");
        public static readonly EventId SecretSkipped = new(1003, "SecretSkipped");
        public static readonly EventId MissingSecretIgnored = new(1004, "MissingSecretIgnored");
        public static readonly EventId DuplicateKeyResolved = new(1005, "DuplicateKeyResolved");
        public static readonly EventId ReloadStarted = new(1010, "ReloadStarted");
        public static readonly EventId ReloadCompleted = new(1011, "ReloadCompleted");
        public static readonly EventId ReloadFailed = new(1012, "ReloadFailed");
    }
}