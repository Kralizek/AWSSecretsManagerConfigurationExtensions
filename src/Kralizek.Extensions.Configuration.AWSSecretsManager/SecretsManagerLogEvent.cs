using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Kralizek.Extensions.Configuration
{
    public sealed record SecretsManagerLogEvent(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception = null,
        IReadOnlyDictionary<string, object?>? Properties = null,
        object?[]? Args = null);
}
