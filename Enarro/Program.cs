using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using Asp.Versioning;
using Enarro.Data;
using Enarro.Extensions;
using Enarro.ServiceDefaults;
using Enarro.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Qdrant.Client;
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

try
{
    #region Add PostgreSQL with EF Core
    
    builder.AddNpgsqlDbContext<EnarroDbContext>("enarro-db");
    
    #endregion Add PostgreSQL
    
    #region Add Redis for Distributed Caching
    
    builder.AddRedisDistributedCache("redis");
    
    #endregion Add Redis

    #region Add Kernel Memory
    
    var chatModel = builder.Configuration.GetValue<string>("RAGConfigs:ChatModel");
    var embeddingModel = builder.Configuration.GetValue<string>("RAGConfigs:EmbeddingModel");

    string GetEndpoint(string name)
    {
        var cs = builder.Configuration.GetConnectionString(name);
        var csBuilder = new DbConnectionStringBuilder { ConnectionString = cs };
        if (!csBuilder.TryGetValue("Endpoint", out var url))
        {  
            throw new InvalidDataException($"{name} connection string is not properly configured.");
        }

        return (string)url;
    }
    
    var ollamaUrl = GetEndpoint("ollama");
    var qdrantGrpcUrl = GetEndpoint("qdrant");
    var qdrantHttpUrl = builder.Configuration.GetValue<string>("RAGConfigs:QdrantEndpoint");
    var qdrantApiKey = builder.Configuration.GetValue<string>("QDRANT_APIKEY");
    
    var ollamaConfig = new OllamaConfig
    {
        TextModel = new OllamaModelConfig(chatModel!) { MaxTokenTotal = 125000, Seed = 42 },
        EmbeddingModel =  new OllamaModelConfig(embeddingModel!) { MaxTokenTotal = 2048 },
        Endpoint = ollamaUrl!
    };

    var memoryBuilder = new KernelMemoryBuilder()
        .WithOllamaTextGeneration(ollamaConfig)
        .WithOllamaTextEmbeddingGeneration(ollamaConfig)
        .WithQdrantMemoryDb((string)qdrantHttpUrl!, qdrantApiKey!)
        .WithSearchClientConfig(new SearchClientConfig { AnswerTokens = 4096});

    var memory = memoryBuilder.Build(new KernelMemoryBuilderBuildOptions
    {
        AllowMixingVolatileAndPersistentData = true
    });

    builder.Services.AddSingleton(memory);
    
    #endregion Add Kernel Memory

    // Register application services
    builder.Services.AddScoped<IConversationService, ConversationService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IDocumentService, DocumentService>();

    builder.Services.AddOpenApi();
    
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
        
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
    builder.Services.AddSwaggerGen();

    // Enhanced Health Checks
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("enarro-db")!, name: "postgresql", tags: ["db", "sql"])
        .AddRedis(builder.Configuration.GetConnectionString("redis")!, name: "redis", tags: ["cache"])
        .AddCheck("ollama", () =>
        {
            using var client = new HttpClient();
            var response = client.GetAsync($"{ollamaUrl}/api/tags").Result;
            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }, tags: ["llm"])
        .AddCheck("qdrant", () =>
        {
            var client = new QdrantClient((string)qdrantGrpcUrl);
            var collection = client.ListCollectionsAsync().Result;
            return HealthCheckResult.Healthy();
        }, tags: ["vector-db"]);
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}

var app = builder.Build();

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EnarroDbContext>();
    await dbContext.Database.MigrateAsync();
}

var apiVersionSet = app
                    .NewApiVersionSet()
                    .HasApiVersion(new ApiVersion(1, 0))
                    .ReportApiVersions()
                    .Build();
var versionedGroup = app
                        .MapGroup("api/v{version:apiVersion}")
                        .WithApiVersionSet(apiVersionSet);

app.MapEndpoints(versionedGroup);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json", 
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.MapDefaultEndpoints();

// Enhanced health check endpoint with detailed JSON response
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var option = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        }, option);
        await context.Response.WriteAsync(result);
    }
});

app.UseHttpsRedirection();

await app.RunAsync();