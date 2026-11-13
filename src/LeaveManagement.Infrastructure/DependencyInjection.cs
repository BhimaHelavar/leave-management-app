using LeaveManagement.Application.Auditing;
using LeaveManagement.Application.Balances;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // "Now" comes from TimeProvider (golden rule); the host also registers it, TryAdd keeps one.
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IBalanceReader, BalanceReader>();
        services.AddScoped<IPendingLeaveReader, PendingLeaveReader>();

        return services;
    }
}
