using System.Text.Json.Serialization;
using Serilog;
using TaskHub.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/taskhub-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerDocumentation();
builder.Services.AddAuthServices();
builder.Services.AddApplicationServices();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddStorageProvider(builder.Configuration);
builder.Services.AddCorsPolicy();

var app = builder.Build();

// Pipeline
app.UseSwaggerDocumentation();
app.UseHttpsRedirection();
app.UseAppMiddleware();

app.Run();

// Make Program accessible for WebApplicationFactory in tests
public partial class Program { }
