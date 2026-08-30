using LeaveManagement.Api.Extensions;
using LeaveManagement.Api.Middleware;
using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Infrastructure;
using LeaveManagement.Infrastructure.Authentication;
using LeaveManagement.Infrastructure.Data;
using LeaveManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using CloudinaryDotNet;

var builder = WebApplication.CreateBuilder(args);

// --- PORT LOCKDOWN: Ensure Kestrel only binds to the single cloud port ---
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out var cloudPort))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(cloudPort);
    });
}

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// 1. Presentation & API Services
builder.Services.AddPresentationServices(builder.Configuration);

// 2. Application Layer Services
builder.Services.AddApplicationServices();

// 3. Infrastructure Layer
builder.Services.AddInfrastructureServices(builder.Configuration);

// --- Health Check Configuration ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// --- Cloudinary Registration & Validation ---
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
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPhotoService, CloudinaryService>();

// 4. JWT Authentication
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddJwtAuthentication(builder.Configuration);

// 5. Rate Limiting Configuration
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

// 6. CORS Configuration
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

app.UseCors("AllowAll");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

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

app.MapControllers();
app.MapHealthChecks("/health");

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