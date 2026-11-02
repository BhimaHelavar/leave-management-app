using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Reference data every environment needs: the one office location and the seven leave types.
/// It runs once after migrations and only adds rows that are missing, so it is safe to run again.
/// </summary>
internal static class ReferenceDataSeeder
{
    public static async Task SeedAsync(LeaveDbContext db, CancellationToken ct = default)
    {
        if (!await db.Locations.AnyAsync(ct))
        {
            // One seeded location for the MVP (3c open question 3).
            db.Locations.Add(new Location
            {
                Name = "Bengaluru",
                CountryCode = "IN",
                TimeZoneId = "Asia/Kolkata",
            });
        }

        if (!await db.LeaveTypes.AnyAsync(ct))
        {
            db.LeaveTypes.AddRange(LeaveTypes());
        }

        await db.SaveChangesAsync(ct);
    }

    // Annual comes straight from the accepted rules: 18 days a year (1.5 a month), carry forward
    // up to 10 days expiring 31 March, notice 3 working days, max 15 in a row, no use in the first
    // 3 months. Sick 10 and Casual 6 are assumptions from the leave-types mock-up (3c open question 4).
    private static IEnumerable<LeaveType> LeaveTypes() =>
    [
        new LeaveType
        {
            Code = "ANNUAL", Name = "Annual leave", IsPaid = true, DaysPerYear = 18.0m,
            AccrualMode = AccrualMode.Monthly, CarryForwardMaxDays = 10.0m,
            CarryForwardExpiryMonth = 3, CarryForwardExpiryDay = 31,
            MinNoticeWorkingDays = 3, MaxConsecutiveDays = 15, ProbationBlockMonths = 3,
            AllowHalfDay = true,
        },
        new LeaveType
        {
            Code = "SICK", Name = "Sick leave", IsPaid = true, DaysPerYear = 10.0m,
            AccrualMode = AccrualMode.Yearly, AllowHalfDay = true, MedicalCertAfterDays = 2,
        },
        new LeaveType
        {
            Code = "CASUAL", Name = "Casual leave", IsPaid = true, DaysPerYear = 6.0m,
            AccrualMode = AccrualMode.Yearly, AllowHalfDay = true,
        },
        new LeaveType
        {
            Code = "COMPOFF", Name = "Compensatory off", IsPaid = true,
            AccrualMode = AccrualMode.Manual, AllowHalfDay = true,
        },
        new LeaveType
        {
            Code = "UNPAID", Name = "Unpaid leave", IsPaid = false,
            AccrualMode = AccrualMode.None, AllowHalfDay = true,
        },
        new LeaveType
        {
            Code = "MATERNITY", Name = "Maternity leave", IsPaid = true,
            AccrualMode = AccrualMode.Manual, AllowHalfDay = false,
        },
        new LeaveType
        {
            Code = "PATERNITY", Name = "Paternity leave", IsPaid = true,
            AccrualMode = AccrualMode.Manual, AllowHalfDay = false,
        },
    ];
}
