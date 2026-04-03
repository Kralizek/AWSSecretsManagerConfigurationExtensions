using Kralizek.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSecretsManager(options =>
{
    options.ReloadInterval = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
