using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Demo data for the dashboard (US-02 / SCRUM-9), Development only. Nobody can apply for or approve
/// leave in the app yet (4d, 4e), so this writes the Jira examples straight into the ledger:
///   Ravi Kumar:  Annual 10 accrued, 2 used, 0 pending -> 8 available   (acceptance criterion 1)
///   Asha Nair:   Annual 10 accrued, 2 used, 3 pending -> 5 available   (acceptance criterion 2)
///   Meera Rao:   Annual 16 accrued, 1 used -> 15 available, 3 of them carry-forward (FR-007)
/// It only adds rows, never resets anything, and skips a person whose scenario already exists,
/// so it is safe to run on every start. The ledger and LeaveBalance are written together, so the
/// reconciliation query (ledger sum equals Available) still returns no rows.
/// </summary>
internal static class DemoBalanceScenarios
{
    public static async Task SeedAsync(LeaveDbContext db, TimeProvider time, CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var year = (short)now.Year;
        var marker = $"Demo scenario SCRUM-9 ({year})";

        var people = await db.Employees
            .AsNoTracking()
            .Where(e => e.Email.EndsWith("@example.test"))
            .ToDictionaryAsync(e => e.Email, e => e.Id, ct);
        var annualId = await db.LeaveTypes
            .Where(t => t.Code == "ANNUAL")
            .Select(t => (int?)t.Id)
            .SingleOrDefaultAsync(ct);

        if (annualId is null
            || !people.TryGetValue("hema.shetty@example.test", out var hrId)
            || !people.TryGetValue("meera.rao@example.test", out var managerId))
        {
            return; // The demo people are not there, so there is nothing to attach the scenarios to.
        }

        // Ravi: the opening 12 becomes 10 (an HR adjustment), then 2 approved days are used.
        await ApplyAsync("ravi.kumar@example.test", alignDays: -2m, approvedDays: 2, pendingDays: 0, carryForward: 0m);

        // Asha: the same, plus one pending request of 3 days.
        await ApplyAsync("asha.nair@example.test", alignDays: -2m, approvedDays: 2, pendingDays: 3, carryForward: 0m);

        // Meera: 4 carried-forward days that lapse on 31 March, and 1 approved day used.
        // Shown: accrued 16, used 1, available 15, of which 3 carry-forward. If 31 March of this year has
        // already passed, the demo uses next year's date so the example stays visible.
        await ApplyAsync("meera.rao@example.test", alignDays: 0m, approvedDays: 1, pendingDays: 0, carryForward: 4m);

        async Task ApplyAsync(string email, decimal alignDays, int approvedDays, int pendingDays, decimal carryForward)
        {
            if (!people.TryGetValue(email, out var employeeId))
            {
                return;
            }

            if (await db.LeaveBalanceEntries.AnyAsync(
                    e => e.EmployeeId == employeeId && e.LeaveYear == year && e.Note == marker, ct))
            {
                return; // Already seeded.
            }

            // Needs the opening balance row of this year (written by DevSeeder); if it is not there, skip.
            if (!await db.LeaveBalances.AnyAsync(
                    b => b.EmployeeId == employeeId && b.LeaveTypeId == annualId && b.LeaveYear == year, ct))
            {
                return;
            }

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var requests = new List<(LeaveRequest Request, int Days)>();
            if (approvedDays > 0)
            {
                requests.Add((NewRequest(employeeId, FirstMondayOnOrAfter(year, 1, 5), approvedDays,
                    LeaveStatus.Approved), approvedDays));
            }

            if (pendingDays > 0)
            {
                requests.Add((NewRequest(employeeId, FirstMondayOnOrAfter(year, 12, 1), pendingDays,
                    LeaveStatus.Pending), pendingDays));
            }

            db.LeaveRequests.AddRange(requests.Select(r => r.Request));
            await db.SaveChangesAsync(ct); // Generates the request ids the ledger rows point at.

            foreach (var (request, days) in requests)
            {
                for (var offset = 0; offset < days; offset++)
                {
                    var date = request.StartDate.AddDays(offset);
                    db.LeaveRequestDays.Add(new LeaveRequestDay
                    {
                        RequestId = request.Id, EmployeeId = employeeId, LeaveDate = date, Slot = Slot.AM,
                    });
                    db.LeaveRequestDays.Add(new LeaveRequestDay
                    {
                        RequestId = request.Id, EmployeeId = employeeId, LeaveDate = date, Slot = Slot.PM,
                    });
                }
            }

            if (alignDays != 0m)
            {
                db.LeaveBalanceEntries.Add(Entry(employeeId, LedgerEntryType.Adjustment, alignDays, null, hrId));
            }

            if (carryForward > 0m)
            {
                var march31 = new DateOnly(year, 3, 31);
                var expires = march31 >= DateOnly.FromDateTime(now) ? march31 : new DateOnly(year + 1, 3, 31);
                var carried = Entry(employeeId, LedgerEntryType.CarryForward, carryForward, null, hrId);
                carried.ExpiresOn = expires;
                db.LeaveBalanceEntries.Add(carried);
            }

            foreach (var (request, days) in requests)
            {
                db.LeaveBalanceEntries.Add(Entry(employeeId, LedgerEntryType.Deduction, -days, request.Id, employeeId));
            }

            await db.SaveChangesAsync(ct);

            // One atomic update by the total change, never read-add-write (database rule).
            var delta = alignDays + carryForward - requests.Sum(r => r.Days);
            await db.LeaveBalances
                .Where(b => b.EmployeeId == employeeId && b.LeaveTypeId == annualId && b.LeaveYear == year)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Available, b => b.Available + delta), ct);

            await tx.CommitAsync(ct);
        }

        LeaveRequest NewRequest(int employeeId, DateOnly start, int days, LeaveStatus status) => new()
        {
            EmployeeId = employeeId,
            LeaveTypeId = annualId.Value,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            HalfDay = HalfDay.None,
            DaysCount = days,
            Status = status,
            SubmittedAtUtc = now,
            // Nobody approves their own request: HR decides for the manager (rule 14).
            DecidedByEmployeeId = status == LeaveStatus.Pending ? null : (employeeId == managerId ? hrId : managerId),
            DecidedAtUtc = status == LeaveStatus.Pending ? null : now,
        };

        LeaveBalanceEntry Entry(int employeeId, LedgerEntryType type, decimal days, int? requestId, int createdBy) => new()
        {
            EmployeeId = employeeId,
            LeaveTypeId = annualId.Value,
            LeaveYear = year,
            EntryType = type,
            Days = days,
            RequestId = requestId,
            Note = marker,
            CreatedAtUtc = now,
            CreatedByEmployeeId = createdBy,
        };
    }

    // Demo requests start on a Monday, so Monday to Wednesday never touches a weekend.
    private static DateOnly FirstMondayOnOrAfter(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
