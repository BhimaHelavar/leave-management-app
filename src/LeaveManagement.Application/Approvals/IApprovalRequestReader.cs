using LeaveManagement.Domain;

namespace LeaveManagement.Application.Approvals;

public sealed record PendingRequestRow(
    int RequestId,
    int EmployeeId,
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    HalfDay HalfDay,
    decimal DaysCount,
    DateTime SubmittedAtUtc);

public sealed record PendingRequestPage(IReadOnlyList<PendingRequestRow> Rows, int TotalCount);

/// <summary>Port for the leave module. Reads leave.* tables only.</summary>
public interface IApprovalRequestReader
{
    /// <summary>
    /// One page of the pending requests of these people, ordered by start date, then submitted time, then id (all
    /// ascending), so a page is repeatable. <paramref name="page"/> starts at 1.
    /// </summary>
    Task<PendingRequestPage> GetPendingPageAsync(
        IReadOnlyCollection<int> employeeIds, int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// The half-day slots these people hold (pending or approved requests, because only those hold day rows)
    /// on dates from <paramref name="from"/> to <paramref name="to"/>, both included.
    /// </summary>
    Task<IReadOnlyList<HeldSlot>> GetHeldSlotsAsync(
        IReadOnlyCollection<int> employeeIds, DateOnly from, DateOnly to, CancellationToken ct);
}
