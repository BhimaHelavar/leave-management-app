using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Fictional demo people for local development only (never run in production).
/// It matches the sign-in picker of phase 4c: a manager with three team members, one HR user and one admin.
/// Opening balances are written as ledger Grant entries plus matching balance rows,
/// so the reconciliation query returns no rows from day one.
/// </summary>
internal static class DevSeeder
{
    public static async Task SeedAsync(LeaveDbContext db, CancellationToken ct = default)
    {
        if (await db.Employees.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var year = (short)now.Year;

        var locationId = (await db.Locations.SingleAsync(l => l.Name == "Bengaluru", ct)).Id;
        var typeIds = await db.LeaveTypes.ToDictionaryAsync(t => t.Code, t => t.Id, ct);

        Employee NewEmployee(string name, string email, DateOnly joined, int? managerId) => new()
        {
            FullName = name,
            Email = email,
            JoinDate = joined,
            LocationId = locationId,
            ManagerId = managerId,
            CreatedAtUtc = now,
        };

        // People without a manager come first, because the team needs the manager's generated id.
        var manager = NewEmployee("Meera Rao", "meera.rao@example.test", new DateOnly(2022, 6, 1), null);
        var hr = NewEmployee("Hema Shetty", "hema.shetty@example.test", new DateOnly(2021, 1, 4), null);
        var admin = NewEmployee("Arjun Das", "arjun.das@example.test", new DateOnly(2020, 3, 2), null);
        db.Employees.AddRange(manager, hr, admin);
        await db.SaveChangesAsync(ct);

        var asha = NewEmployee("Asha Nair", "asha.nair@example.test", new DateOnly(2023, 2, 13), manager.Id);
        var ravi = NewEmployee("Ravi Kumar", "ravi.kumar@example.test", new DateOnly(2024, 7, 1), manager.Id);
        // Joined a month ago, so the probation rule (no Annual leave in the first 3 months) can be demonstrated.
        var sneha = NewEmployee("Sneha Iyer", "sneha.iyer@example.test", today.AddMonths(-1), manager.Id);
        db.Employees.AddRange(asha, ravi, sneha);
        await db.SaveChangesAsync(ct);

        db.EmployeeRoles.AddRange(
            new EmployeeRole { EmployeeId = manager.Id, Role = Role.Manager },
            new EmployeeRole { EmployeeId = hr.Id, Role = Role.HR },
            new EmployeeRole { EmployeeId = admin.Id, Role = Role.Admin });

        (string Code, decimal Days)[] openingGrants = [("ANNUAL", 12.0m), ("SICK", 10.0m), ("CASUAL", 6.0m)];
        foreach (var employee in new[] { manager, hr, admin, asha, ravi, sneha })
        {
            foreach (var (code, days) in openingGrants)
            {
                db.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = employee.Id,
                    LeaveTypeId = typeIds[code],
                    LeaveYear = year,
                    Available = days,
                });
                db.LeaveBalanceEntries.Add(new LeaveBalanceEntry
                {
                    EmployeeId = employee.Id,
                    LeaveTypeId = typeIds[code],
                    LeaveYear = year,
                    EntryType = LedgerEntryType.Grant,
                    Days = days,
                    Note = "Opening balance (demo data)",
                    CreatedAtUtc = now,
                });
            }
        }

        // Fixed-date holidays only. The full list (festivals move every year) is entered by an Admin.
        (int Month, int Day, string Name)[] holidays =
        [
            (1, 26, "Republic Day"),
            (5, 1, "May Day"),
            (8, 15, "Independence Day"),
            (10, 2, "Gandhi Jayanti"),
            (11, 1, "Kannada Rajyotsava"),
            (12, 25, "Christmas"),
        ];
        foreach (var (month, day, name) in holidays)
        {
            db.Holidays.Add(new Holiday
            {
                LocationId = locationId,
                HolidayDate = new DateOnly(year, month, day),
                Name = name,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
