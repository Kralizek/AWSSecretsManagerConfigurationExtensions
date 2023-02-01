var builder = WebApplication.CreateBuilder(args);

var secretsManagerConfiguration = builder.Configuration.GetSection("SecretsManager").GetSecretsManagerOptions();
secretsManagerConfiguration.PollingInterval = TimeSpan.FromSeconds(10);

builder.Configuration.AddSecretsManager(secretsManagerConfiguration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
