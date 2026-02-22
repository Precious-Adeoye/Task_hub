using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Services;
using Task_hub.Application.Validators;
using TaskHub.Infrastructure.Storage;

namespace TaskHub.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganisationContext, OrganisationContext>();
        services.AddScoped<IImportExportService, ImportExportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IMigrationService, MigrationService>();
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    public static IServiceCollection AddStorageProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var storageProvider = configuration.GetValue<string>("StorageProvider");
        if (storageProvider?.ToLower() == "file")
        {
            services.AddSingleton<IStorage, FileStorage>();

            var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            Log.Information("Using File Storage Provider");
        }
        else
        {
            services.AddSingleton<IStorage, InMemoryStorage>();
            Log.Information("Using In-Memory Storage Provider");
        }

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000")
                      .AllowCredentials()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
