using Kralizek.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using var bootstrapLoggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
});

var builder = new ConfigurationBuilder();

builder.AddSecretsManager(options =>
{
    options.UseBootstrapLogging(bootstrapLoggerFactory);
    options.SecretIds.Add("my-app/prod");
});

var configuration = builder.Build();
