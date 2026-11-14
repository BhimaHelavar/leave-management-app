using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Application;

/// <summary>
/// Application layer: use cases (commands and queries), validation and interfaces
/// that Infrastructure implements. It depends only on Domain.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases and validators are registered here.
        services.AddScoped<Balances.GetMyBalances>();
        services.AddScoped<Leave.LeavePlanner>();
        services.AddScoped<Leave.ApplyForLeave>();
        services.AddScoped<Leave.PreviewWorkingDays>();
        return services;
    }
}
