using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

public sealed class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : DbContext(options)
{
    // people
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();

    // balances
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveBalanceEntry> LeaveBalanceEntries => Set<LeaveBalanceEntry>();

    // leave
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveRequestDay> LeaveRequestDays => Set<LeaveRequestDay>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    // notify and audit
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every timestamp is datetime2(3) in UTC (3b decision 7).
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("datetime2(3)")
            .HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>()
            .HaveColumnType("datetime2(3)")
            .HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeaveDbContext).Assembly);
    }
}
