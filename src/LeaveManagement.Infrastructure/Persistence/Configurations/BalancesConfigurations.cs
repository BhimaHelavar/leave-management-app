using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

// Schema "balances": leave policy and balances.

public sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> b)
    {
        b.ToTable("LeaveType", "balances", t =>
        {
            t.HasCheckConstraint("CK_LeaveType_AccrualMode", EnumColumn.InList<AccrualMode>("AccrualMode"));
            t.HasCheckConstraint(
                "CK_LeaveType_Numbers",
                "([DaysPerYear] IS NULL OR [DaysPerYear] >= 0) " +
                "AND [CarryForwardMaxDays] >= 0 " +
                "AND [MinNoticeWorkingDays] >= 0 " +
                "AND ([MaxConsecutiveDays] IS NULL OR [MaxConsecutiveDays] > 0) " +
                "AND [ProbationBlockMonths] >= 0 " +
                "AND ([MedicalCertAfterDays] IS NULL OR [MedicalCertAfterDays] > 0)");
            // Month and day are both set or both empty.
            t.HasCheckConstraint(
                "CK_LeaveType_CarryForwardExpiry",
                "([CarryForwardExpiryMonth] IS NULL AND [CarryForwardExpiryDay] IS NULL) " +
                "OR ([CarryForwardExpiryMonth] BETWEEN 1 AND 12 AND [CarryForwardExpiryDay] BETWEEN 1 AND 31)");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.DaysPerYear).HasPrecision(5, 1);
        b.Property(x => x.AccrualMode).AsText(10);
        b.Property(x => x.CarryForwardMaxDays).HasPrecision(5, 1);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> b)
    {
        b.ToTable("Holiday", "balances");
        b.HasKey(x => x.Id);
        b.Property(x => x.HolidayDate).HasColumnType("date");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();

        // No duplicate holiday per location (screen S-09 rule). Also the range-seek index for working-day counts.
        b.HasIndex(x => new { x.LocationId, x.HolidayDate }).IsUnique();

        b.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> b)
    {
        // The second line of defence: even buggy code cannot push a balance below zero (rule 5).
        b.ToTable("LeaveBalance", "balances", t =>
            t.HasCheckConstraint("CK_LeaveBalance_Available", "[Available] >= 0"));

        b.HasKey(x => x.Id);
        b.Property(x => x.Available).HasPrecision(5, 1);
        b.Property(x => x.RowVersion).IsRowVersion();

        // One balance row per person, type and year. This unique key is also the dashboard lookup.
        b.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.LeaveYear }).IsUnique();

        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LeaveBalanceEntryConfiguration : IEntityTypeConfiguration<LeaveBalanceEntry>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceEntry> b)
    {
        b.ToTable("LeaveBalanceEntry", "balances", t =>
            t.HasCheckConstraint("CK_LeaveBalanceEntry_EntryType", EnumColumn.InList<LedgerEntryType>("EntryType")));

        b.HasKey(x => x.Id);
        b.Property(x => x.EntryType).AsText(15);
        b.Property(x => x.Days).HasPrecision(5, 1);
        b.Property(x => x.ExpiresOn).HasColumnType("date");
        b.Property(x => x.Note).HasMaxLength(200);

        // Used by the reconciliation query: sum of one person's ledger.
        b.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.LeaveYear }).IncludeProperties(x => x.Days);

        // Used by reversals (restore the days a request took).
        b.HasIndex(x => x.RequestId).HasFilter("[RequestId] IS NOT NULL");

        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LeaveRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
    }
}
