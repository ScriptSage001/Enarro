using Enarro;
using Enarro.Application;
using Enarro.Infrastructure;
using Enarro.Persistence;
using Enarro.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/enarro-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
});

builder.AddServiceDefaults();

// ─── Layer Registration ─────────────────────────────
builder.Services.AddApplication();
builder.AddPersistence();
builder.AddInfrastructure();
builder.Services.AddApi();

// ─── Build & Configure Pipeline ─────────────────────
var app = builder.Build();

await app.UseApi();

await app.RunAsync();