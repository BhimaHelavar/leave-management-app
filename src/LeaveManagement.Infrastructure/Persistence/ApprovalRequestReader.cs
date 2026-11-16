using LeaveManagement.Application.Approvals;
using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Reads the leave module (LeaveRequest, LeaveRequestDay) for a team. Touches leave.* only.</summary>
internal sealed class ApprovalRequestReader(LeaveDbContext db) : IApprovalRequestReader
{
    public async Task<PendingRequestPage> GetPendingPageAsync(
        IReadOnlyCollection<int> employeeIds, int page, int pageSize, CancellationToken ct)
    {
        var pending = db.LeaveRequests
            .AsNoTracking()
            .Where(r => r.Status == LeaveStatus.Pending && employeeIds.Contains(r.EmployeeId));

        var total = await pending.CountAsync(ct);

        // Work in long so a huge page cannot overflow; the endpoint also caps page.
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        // A full order (start date, submitted time, id) makes every page repeatable.
        var rows = await pending
            .OrderBy(r => r.StartDate)
            .ThenBy(r => r.SubmittedAtUtc)
            .ThenBy(r => r.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new PendingRequestRow(
                r.Id, r.EmployeeId, r.LeaveTypeId, r.StartDate, r.EndDate, r.HalfDay, r.DaysCount, r.SubmittedAtUtc))
            .ToListAsync(ct);

        return new PendingRequestPage(rows, total);
    }

    public async Task<RequestDetailRow?> GetByIdAsync(int requestId, CancellationToken ct)
    {
        // Any status on purpose: the decision use case tells "not pending" (409) from "not found" (404).
        return await db.LeaveRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new RequestDetailRow(
                r.Id, r.EmployeeId, r.LeaveTypeId, r.StartDate, r.EndDate, r.HalfDay, r.DaysCount,
                r.SubmittedAtUtc, r.Status, r.DecidedByEmployeeId, r.DecidedAtUtc, r.DecisionComment))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<HeldSlot>> GetHeldSlotsAsync(
        IReadOnlyCollection<int> employeeIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        // A day row exists only while a request is pending or approved, so these rows are exactly the held days.
        return await db.LeaveRequestDays
            .AsNoTracking()
            .Where(d => employeeIds.Contains(d.EmployeeId) && d.LeaveDate >= from && d.LeaveDate <= to)
            .Select(d => new HeldSlot(d.RequestId, d.EmployeeId, d.LeaveDate, d.Slot))
            .ToListAsync(ct);
    }
}
