using System.Data.Common;
using System.Text;
using Enarro.Application.Abstractions;
using Enarro.Infrastructure.AI;
using Enarro.Infrastructure.Auth;
using Enarro.Infrastructure.Cache;
using Enarro.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;

namespace Enarro.Infrastructure;

/// <summary>
/// Dependency injection registration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services: JWT auth, Kernel Memory, Redis, health checks.
    /// </summary>
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        // ───────────────────────────── Auth ─────────────────────────────
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
        AddJwtAuthentication(builder.Services, configuration);

        // ───────────────────────────── AI / Vector Memory ─────────────────────────────
        builder.Services.AddSingleton<IVectorMemoryService, KernelMemoryVectorService>();
        AddKernelMemory(builder.Services, configuration);

        // ───────────────────────────── Aspire-managed Redis ─────────────────────────────
        builder.AddRedisDistributedCache("redis");
        builder.Services.AddSingleton<IConversationStore, RedisConversationStore>();

        // ───────────────────────────── HTTP Client Factory ─────────────────────────────
        builder.Services.AddHttpClient();

        // ───────────────────────────── Health Checks ─────────────────────────────
        AddHealthChecks(builder.Services, configuration);

        return builder;
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured. Use user secrets.");
        var jwtIssuer = configuration["JwtSettings:Issuer"] ?? "Enarro";
        var jwtAudience = configuration["JwtSettings:Audience"] ?? "EnarroAPI";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
        });
    }

    private static void AddKernelMemory(IServiceCollection services, IConfiguration configuration)
    {
        var chatModel = configuration.GetValue<string>("RAGConfigs:ChatModel");
        var embeddingModel = configuration.GetValue<string>("RAGConfigs:EmbeddingModel");
        var qdrantHttpUrl = configuration.GetValue<string>("RAGConfigs:QdrantEndpoint");
        var qdrantApiKey = configuration.GetValue<string>("QDRANT_APIKEY");

        var ollamaUrl = GetEndpoint(configuration, "ollama");

        var ollamaConfig = new OllamaConfig
        {
            TextModel = new OllamaModelConfig(chatModel!) { MaxTokenTotal = 125000, Seed = 42 },
            EmbeddingModel = new OllamaModelConfig(embeddingModel!) { MaxTokenTotal = 2048 },
            Endpoint = ollamaUrl!
        };

        var memoryBuilder = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(ollamaConfig)
            .WithOllamaTextEmbeddingGeneration(ollamaConfig)
            .WithQdrantMemoryDb(qdrantHttpUrl!, qdrantApiKey!)
            .WithSearchClientConfig(new SearchClientConfig { AnswerTokens = 4096 });

        var memory = memoryBuilder.Build(new KernelMemoryBuilderBuildOptions
        {
            AllowMixingVolatileAndPersistentData = true
        });

        services.AddSingleton(memory);
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("redis");
        var healthChecks = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecks.AddRedis(redisConnectionString, name: "redis", tags: ["cache"]);
        }

        healthChecks
            .AddCheck<OllamaHealthCheck>("ollama", tags: ["llm"])
            .AddCheck<QdrantHealthCheck>("qdrant", tags: ["vector-db"]);
    }

    /// <summary>
    /// Extracts the Endpoint value from an Aspire-style connection string.
    /// </summary>
    internal static string GetEndpoint(IConfiguration configuration, string name)
    {
        var cs = configuration.GetConnectionString(name);
        var csBuilder = new DbConnectionStringBuilder { ConnectionString = cs };
        if (!csBuilder.TryGetValue("Endpoint", out var url))
        {
            throw new InvalidDataException($"{name} connection string is not properly configured.");
        }

        return (string)url;
    }
}
