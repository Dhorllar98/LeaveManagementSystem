using LeaveManagement.Api.Extensions;
using LeaveManagement.Api.Middleware;
using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Infrastructure;
using LeaveManagement.Infrastructure.Authentication;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Presentation & API Services
builder.Services.AddPresentationServices(builder.Configuration);

// 2. Application Layer Services & Validators
builder.Services.AddApplicationServices();

// 3. Infrastructure Layer (Data, Repositories, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

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

var app = builder.Build();

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

app.Run();