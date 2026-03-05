using System.Reflection;
using System.Text.Json;
using Asp.Versioning;
using Enarro.Application.Abstractions;
using Enarro.Extensions;
using Enarro.Middleware;
using Enarro.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Enarro.ServiceDefaults;

namespace Enarro;

/// <summary>
/// Dependency injection registration for the API layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers API-level services: CurrentUserService, exception handling, OpenApi, versioning, endpoints.
    /// </summary>
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // CurrentUserService (scoped, implements ICurrentUserService)
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());

        // Global exception handler
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // OpenAPI + Swagger
        services.AddOpenApi();
        services.AddSwaggerGen();
        services.AddEndpointsApiExplorer();

        // API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Minimal API endpoint discovery
        services.AddEndpoints(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>
    /// Configures the API middleware pipeline: exception handling, auth, endpoints, Swagger, health checks.
    /// </summary>
    public static async Task<WebApplication> UseApi(this WebApplication app)
    {
        // Run database migrations
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.EnarroDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // Exception handling
        app.UseExceptionHandler();

        // Authentication & authorization
        app.UseAuthentication();
        app.UseMiddleware<UserContextMiddleware>();
        app.UseAuthorization();

        // API versioning + endpoint mapping
        var apiVersionSet = app
            .NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var versionedGroup = app
            .MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(apiVersionSet);

        app.MapEndpoints(versionedGroup);

        // Swagger (development only)
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

        // Aspire defaults
        app.MapDefaultEndpoints();

        // Health check endpoint with detailed JSON response
        app.MapHealthChecks("/health-check", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var options = new JsonSerializerOptions { WriteIndented = true };
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
                }, options);
                await context.Response.WriteAsync(result);
            }
        });

        app.UseHttpsRedirection();

        return app;
    }
}