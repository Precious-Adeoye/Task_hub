using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Task_hub.Application.Abstraction;
using Task_hub.Application.Auth;
using Task_hub.Application.Authorization;
using Task_hub.Application.Migration;
using Task_hub.Application.Service;
using TaskHub.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/taskhub-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
// Add services
builder.Services.AddControllers();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "TaskHub.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganisationContext, OrganisationContext>();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrgMember", policy =>
        policy.Requirements.Add(new OrganisationRequirement(requireAdmin: false)));

    options.AddPolicy("OrgAdmin", policy =>
        policy.Requirements.Add(new OrganisationRequirement(requireAdmin: true)));
});
builder.Services.AddSingleton<IAuthorizationHandler, OrganisationAuthorizationHandler>();

// Add this to the services section
builder.Services.AddSingleton<IMigrationService, MigrationService>();

// Storage - switchable via configuration
var storageProvider = builder.Configuration.GetValue<string>("StorageProvider");
if (storageProvider?.ToLower() == "file")
{
    builder.Services.AddSingleton<IStorage, FileStorage>();

    // Ensure storage directory exists
    var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
    if (!Directory.Exists(storagePath))
    {
        Directory.CreateDirectory(storagePath);
    }

    Log.Information("Using File Storage Provider");
}
else
{
    builder.Services.AddSingleton<IStorage, InMemoryStorage>();
    Log.Information("Using In-Memory Storage Provider");
}

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

