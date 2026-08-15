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

var builder = WebApplication.CreateBuilder(args);

// --- PORT LOCKDOWN: Ensure Kestrel only binds to the single cloud port ---
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out var cloudPort))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Closes all other default internal ports and binds exclusively to the cloud port
        options.ListenAnyIP(cloudPort);
    });
}

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// 1. Presentation & API Services (Controllers, JSON options, Swagger)
builder.Services.AddPresentationServices(builder.Configuration);

// 2. Application Layer Services, Validators & Department logic
builder.Services.AddApplicationServices();

// 3. Infrastructure Layer (Data, Repositories, DbContext)
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 4. JWT Authentication & Options Binding
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

var app = builder.Build();

app.UseCors("AllowAll");

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Leave Management API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();