using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies migrations, then adds reference data, then (development only) demo data.
    /// Per the 3b decision, this runs at startup in Development only. Elsewhere migrations
    /// run as a separate step with a separate, more powerful database login.
    /// </summary>
    public static async Task InitializeAsync(
        IServiceProvider services, bool seedDemoData, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();

        if (!db.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "No EF Core migrations were found, so the database cannot be created. Run this once from the " +
                "repository root: dotnet ef migrations add Initial " +
                "--project src/LeaveManagement.Infrastructure --startup-project src/LeaveManagement.Api " +
                "--output-dir Persistence/Migrations");
        }

        await db.Database.MigrateAsync(ct);
        await ReferenceDataSeeder.SeedAsync(db, ct);

        if (seedDemoData)
        {
            await DevSeeder.SeedAsync(db, ct);
            await DemoBalanceScenarios.SeedAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>(), ct);
        }
    }
}
