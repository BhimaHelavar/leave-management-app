using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Infrastructure;

/// <summary>
/// Infrastructure layer: EF Core, SQL Server, identity and later Redis and RabbitMQ.
/// It implements interfaces defined in Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default is not set. See appsettings.Development.json or the compose file.");

        services.AddDbContext<LeaveDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
