using LeaveManagement.Application.Balances;
using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Reads the balances module (LeaveType, LeaveBalance, LeaveBalanceEntry) for one person and year.
/// Three small queries are merged in memory: one person has at most a handful of leave types,
/// and plain queries are easier to read and to translate than one large grouped join.
/// </summary>
internal sealed class BalanceReader(LeaveDbContext db) : IBalanceReader
{
    public async Task<IReadOnlyList<LeaveTypeLedger>> GetLedgerAsync(
        int employeeId, short leaveYear, DateOnly today, CancellationToken ct)
    {
        var types = await db.LeaveTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .Select(t => new { t.Id, t.Code, t.Name, t.AccrualMode })
            .ToListAsync(ct);

        var available = await db.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId && b.LeaveYear == leaveYear)
            .ToDictionaryAsync(b => b.LeaveTypeId, b => b.Available, ct);

        var entries = await db.LeaveBalanceEntries
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId && e.LeaveYear == leaveYear)
            .Select(e => new { e.LeaveTypeId, e.EntryType, e.Days, e.ExpiresOn })
            .ToListAsync(ct);

        var byType = entries.ToLookup(e => e.LeaveTypeId);

        var result = new List<LeaveTypeLedger>(types.Count);
        foreach (var type in types)
        {
            var rows = byType[type.Id].ToList();

            // Deductions are negative and restores positive, so held comes out positive.
            var held = -rows
                .Where(r => r.EntryType is LedgerEntryType.Deduction or LedgerEntryType.Restore)
                .Sum(r => r.Days);

            var liveCarryForward = rows
                .Where(r => r.EntryType == LedgerEntryType.CarryForward
                    && r.ExpiresOn is not null && r.ExpiresOn >= today)
                .ToList();

            result.Add(new LeaveTypeLedger(
                type.Id,
                type.Code,
                type.Name,
                type.AccrualMode,
                available.TryGetValue(type.Id, out var value) ? value : null,
                held,
                liveCarryForward.Sum(r => r.Days),
                liveCarryForward.Count == 0 ? null : liveCarryForward.Min(r => r.ExpiresOn)));
        }

        return result;
    }
}
