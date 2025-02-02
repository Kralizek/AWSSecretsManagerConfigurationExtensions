var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSecretsManager(configurator: options => options.PollingInterval = TimeSpan.FromSeconds(10));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();