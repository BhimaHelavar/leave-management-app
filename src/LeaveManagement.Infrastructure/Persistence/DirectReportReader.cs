using LeaveManagement.Application.Approvals;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Reads the people module (Employee) for one manager's team. Touches people.* only.</summary>
internal sealed class DirectReportReader(LeaveDbContext db) : IDirectReportReader
{
    public async Task<IReadOnlyList<ReportInfo>> GetAsync(int managerId, CancellationToken ct)
    {
        // Read on every call: a changed reporting line or a deactivated person applies at once.
        // The manager is never in their own list, even if their own ManagerId points at themself.
        return await db.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == managerId && e.IsActive && e.Id != managerId)
            .OrderBy(e => e.FullName)
            .ThenBy(e => e.Id)
            .Select(e => new ReportInfo(e.Id, e.FullName))
            .ToListAsync(ct);
    }
}
