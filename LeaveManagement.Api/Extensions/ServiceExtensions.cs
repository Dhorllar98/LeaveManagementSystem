using FluentValidation;
using LeaveManagement.Application.DTOs.Auth;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Application.Services;
using LeaveManagement.Application.Validators;
using LeaveManagement.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace LeaveManagement.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste your JWT token directly below (do NOT type 'Bearer ')."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Authentication & Tokens
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Domain Services
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ILeaveAllocationService, LeaveAllocationService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IUserService, UserService>();

        // Fluent Validators (Auto-scans and registers all validators in the Application assembly)
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "JWT Secret is missing! Ensure 'JwtSettings:Secret' is set in User Secrets or appsettings.json.");
        }

        var issuer = jwtSettings["Issuer"] ?? "LeaveManagementApi";
        var audience = jwtSettings["Audience"] ?? "LeaveManagementClient";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero
                };

                // DIAGNOSTIC EVENTS: Catches and prints authentication failures to the Output window
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n================ [JWT AUTHENTICATION FAILED] ================");
                        Console.WriteLine($"Exception Type: {context.Exception.GetType().Name}");
                        Console.WriteLine($"Error Message:  {context.Exception.Message}");
                        Console.WriteLine("=============================================================\n");
                        Console.ResetColor();
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\n================ [JWT CHALLENGE TRIGGERED] ================");
                        Console.WriteLine($"Error:             {context.Error}");
                        Console.WriteLine($"Error Description: {context.ErrorDescription}");
                        Console.WriteLine("===========================================================\n");
                        Console.ResetColor();
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}