// Use case: I need logging while configuration is being built, before the DI container is ready.
//
// Create a LoggerFactory before building configuration and pass it to UseBootstrapLogging.
// This lets the Secrets Manager provider emit structured log events through Microsoft.Extensions.Logging
// even though the DI container has not been set up yet.

using Kralizek.Extensions.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using var bootstrapLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
});

var builder = new ConfigurationBuilder();

builder.AddSecretsManagerKnownSecret("my-app/prod", options =>
{
    options.UseBootstrapLogging(bootstrapLoggerFactory);
});

builder.Build();