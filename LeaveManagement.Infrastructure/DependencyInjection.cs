using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using LeaveManagement.Infrastructure.Repositories;
using LeaveManagement.Infrastructure.Services; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveAllocationRepository, LeaveAllocationRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Infrastructure Services
        services.AddScoped<IPhotoService, CloudinaryService>(); 

        return services;
    }
}