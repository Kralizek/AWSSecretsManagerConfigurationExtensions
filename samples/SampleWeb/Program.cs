var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSecretsManager(options => options.Configure(o => o.PollingInterval = TimeSpan.FromSeconds(10)));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
