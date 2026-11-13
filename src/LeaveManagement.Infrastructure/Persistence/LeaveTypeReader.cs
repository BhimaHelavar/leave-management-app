using LeaveManagement.Application.Leave;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Reads the policy numbers of one balances.LeaveType.</summary>
internal sealed class LeaveTypeReader(LeaveDbContext db) : ILeaveTypeReader
{
    public Task<LeaveTypeRules?> GetAsync(int leaveTypeId, CancellationToken ct) =>
        db.LeaveTypes
            .AsNoTracking()
            .Where(t => t.Id == leaveTypeId)
            .Select(t => new LeaveTypeRules(
                t.Id, t.IsActive, t.AccrualMode, t.AllowHalfDay, t.MinNoticeWorkingDays, t.MaxConsecutiveDays))
            .FirstOrDefaultAsync(ct);
}
