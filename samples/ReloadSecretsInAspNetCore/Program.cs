// Use case: I want an ASP.NET Core application to reload secrets periodically.
//
// Set ReloadInterval to enable background polling. The provider will re-fetch secrets
// and update the IConfiguration values at the specified interval without restarting
// the application.

using Kralizek.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSecretsManagerDiscovery(options =>
{
    options.ReloadInterval = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();