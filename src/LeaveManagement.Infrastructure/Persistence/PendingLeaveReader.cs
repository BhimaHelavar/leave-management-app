using LeaveManagement.Application.Balances;
using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Reads the leave module (LeaveRequest) for the days still awaiting a decision.</summary>
internal sealed class PendingLeaveReader(LeaveDbContext db) : IPendingLeaveReader
{
    public async Task<IReadOnlyDictionary<int, decimal>> GetPendingDaysAsync(
        int employeeId, short leaveYear, CancellationToken ct)
    {
        // DaysCount is the snapshot taken at submit; it is never recalculated.
        var rows = await db.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId
                && r.LeaveYear == leaveYear
                && r.Status == LeaveStatus.Pending)
            .Select(r => new { r.LeaveTypeId, r.DaysCount })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.LeaveTypeId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DaysCount));
    }
}
