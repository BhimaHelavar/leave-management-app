using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

// Schema "leave".

public sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        // The database refuses a bad row even if the code has a bug.
        b.ToTable("LeaveRequest", "leave", t =>
        {
            t.HasCheckConstraint("CK_LeaveRequest_Status", EnumColumn.InList<LeaveStatus>("Status"));
            t.HasCheckConstraint("CK_LeaveRequest_HalfDayValue", EnumColumn.InList<HalfDay>("HalfDay"));
            // A request does not cross a leave year (3c open question 1: block in the MVP).
            t.HasCheckConstraint(
                "CK_LeaveRequest_Dates",
                "[EndDate] >= [StartDate] AND YEAR([StartDate]) = YEAR([EndDate])");
            // Rule 11: a half day is a single day.
            t.HasCheckConstraint("CK_LeaveRequest_HalfDay", "[HalfDay] = 'None' OR [StartDate] = [EndDate]");
            t.HasCheckConstraint("CK_LeaveRequest_Days", "[DaysCount] > 0");
            // Story US-07: a rejection must say why.
            t.HasCheckConstraint(
                "CK_LeaveRequest_RejectComment",
                "[Status] <> 'Rejected' OR [DecisionComment] IS NOT NULL");
            // A pending request has no decision time; every other status has one.
            t.HasCheckConstraint(
                "CK_LeaveRequest_DecidedAt",
                "([Status] = 'Pending' AND [DecidedAtUtc] IS NULL) OR ([Status] <> 'Pending' AND [DecidedAtUtc] IS NOT NULL)");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.StartDate).HasColumnType("date");
        b.Property(x => x.EndDate).HasColumnType("date");
        b.Property(x => x.HalfDay).AsText(10);
        b.Property(x => x.Status).AsText(10);
        b.Property(x => x.DaysCount).HasPrecision(4, 1);
        b.Property(x => x.LeaveYear).HasComputedColumnSql("CONVERT(smallint, YEAR([StartDate]))", stored: true);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.DecisionComment).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        // Target of the composite foreign key from LeaveRequestDay.
        b.HasAlternateKey(x => new { x.Id, x.EmployeeId });

        // My requests (S-04): newest start first. Covering, so the list never touches the table.
        b.HasIndex(x => new { x.EmployeeId, x.StartDate })
            .IsDescending(false, true)
            .IncludeProperties(x => new { x.LeaveTypeId, x.Status, x.DaysCount, x.EndDate, x.HalfDay });

        // Manager inbox (S-05): pending requests by start date. Filtered, so it stays tiny.
        b.HasIndex(x => x.StartDate)
            .HasDatabaseName("IX_LeaveRequest_Pending_StartDate")
            .HasFilter("[Status] = 'Pending'")
            .IncludeProperties(x => new { x.EmployeeId, x.LeaveTypeId, x.DaysCount, x.EndDate });

        // HR reports (S-07): approved days by type in a date range.
        b.HasIndex(x => new { x.Status, x.StartDate })
            .IncludeProperties(x => new { x.EmployeeId, x.LeaveTypeId, x.DaysCount });

        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LeaveRequestDayConfiguration : IEntityTypeConfiguration<LeaveRequestDay>
{
    public void Configure(EntityTypeBuilder<LeaveRequestDay> b)
    {
        b.ToTable("LeaveRequestDay", "leave", t =>
            t.HasCheckConstraint("CK_LeaveRequestDay_Slot", EnumColumn.InList<Slot>("Slot")));

        b.HasKey(x => new { x.RequestId, x.LeaveDate, x.Slot });
        b.Property(x => x.LeaveDate).HasColumnType("date");
        b.Property(x => x.Slot).AsText(2);

        // Rule 13: the same person cannot have two leaves on the same half-day. The database settles the race.
        b.HasIndex(x => new { x.EmployeeId, x.LeaveDate, x.Slot })
            .IsUnique()
            .HasDatabaseName("UX_LeaveRequestDay_Employee_Date_Slot");

        // Team overlap (S-05, S-06): who is away on these dates.
        b.HasIndex(x => x.LeaveDate).IncludeProperties(x => x.EmployeeId);

        // The copy of EmployeeId can never disagree with the request it belongs to.
        b.HasOne<LeaveRequest>()
            .WithMany()
            .HasForeignKey(x => new { x.RequestId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.Id, x.EmployeeId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> b)
    {
        b.ToTable("IdempotencyKey", "leave");

        // Named KeyValue because KEY is a reserved word in T-SQL.
        b.HasKey(x => new { x.EmployeeId, x.KeyValue });
        b.Property(x => x.KeyValue).HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.ResponseJson).HasColumnType("nvarchar(max)");

        // Cleanup job: delete keys older than a day, in small batches.
        b.HasIndex(x => x.CreatedAtUtc);

        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
