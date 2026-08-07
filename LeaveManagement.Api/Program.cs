using LeaveManagement.Api.Extensions;
using LeaveManagement.Api.Middleware;
using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Infrastructure;
using LeaveManagement.Infrastructure.Authentication;
using LeaveManagement.Infrastructure.Data;
using LeaveManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization; 
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Presentation & API Services
builder.Services.AddPresentationServices(builder.Configuration);

// Configure Controllers to ignore object reference cycles globally
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 2. Application Layer Services & Validators
builder.Services.AddApplicationServices();

// 3. Infrastructure Layer (Data, Repositories, etc.)
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