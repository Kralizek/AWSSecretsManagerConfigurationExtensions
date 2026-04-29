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

var configuration = builder.Build();
