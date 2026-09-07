using CloudinaryDotNet;
using LeaveManagement.Api.Extensions;
using LeaveManagement.Api.Hubs;
using LeaveManagement.Api.Middleware;
using LeaveManagement.Application.Common.Models;
using LeaveManagement.Infrastructure;
using LeaveManagement.Infrastructure.Data;
using LeaveManagement.Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Kestrel Cloud Port Binding
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out var cloudPort))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(cloudPort);
    });
}

// 2. Configuration Setup (Preserving User Secrets for Local Dev)
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Configuration.AddEnvironmentVariables();

// 3. Configure Forwarded Headers for Cloud Reverse Proxies
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 4. Core Service Registrations
builder.Services.AddPresentationServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// 5. Cloudinary Registration
var cloudName = builder.Configuration["CloudinarySettings:CloudName"] ?? builder.Configuration["Cloudinary:CloudName"];
var apiKey = builder.Configuration["CloudinarySettings:ApiKey"] ?? builder.Configuration["Cloudinary:ApiKey"];
var apiSecret = builder.Configuration["CloudinarySettings:ApiSecret"] ?? builder.Configuration["Cloudinary:ApiSecret"];

if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
{
    throw new InvalidOperationException("CRITICAL: Cloudinary settings (CloudName, ApiKey, ApiSecret) are missing from configuration.");
}

var cloudinaryAccount = new Account(cloudName, apiKey, apiSecret);
var cloudinary = new Cloudinary(cloudinaryAccount) { Api = { Secure = true } };

builder.Services.AddSingleton(cloudinary);

// 6. JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddJwtAuthentication(builder.Configuration);

// 7. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                QueueLimit = 2,
                Window = TimeSpan.FromSeconds(10)
            }));
});

// 8. CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddMemoryCache();

var app = builder.Build();

// --- MIDDLEWARE PIPELINE ---

app.UseForwardedHeaders(); // Must run first to resolve real client IP behind reverse proxies
app.UseCors("AllowAll");
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Leave Management API V1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Idempotency runs after auth so it can inspect User claims if needed
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<NotificationHub>("/hubs/notifications");

// --- DATABASE MIGRATION & SEEDING ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying pending database migrations...");
        await context.Database.MigrateAsync();

        logger.LogInformation("Seeding database initial data...");
        await DbInitializer.SeedAsync(context);

        logger.LogInformation("Database migration and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "CRITICAL: An error occurred while migrating or seeding the database.");
    }
}

app.Run();