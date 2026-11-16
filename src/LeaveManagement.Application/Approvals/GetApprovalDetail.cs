using LeaveManagement.Domain;

namespace LeaveManagement.Application.Approvals;

/// <summary>
/// Use case: one request of a direct report (US-07). A missing request and a request of someone who is not a
/// direct report both return null, so the caller cannot tell them apart and cannot probe ids (security rule 4).
/// It only reads.
/// </summary>
public sealed class GetApprovalDetail(
    IDirectReportReader reports,
    IApprovalRequestReader requests,
    ApprovalItemBuilder builder)
{
    public async Task<ApprovalDetail?> ExecuteAsync(int managerId, int requestId, CancellationToken ct)
    {
        // The team is looked up now, never taken from the token (security rule 7).
        var team = await reports.GetAsync(managerId, ct);
        var row = await requests.GetByIdAsync(requestId, ct);
        if (row is null || team.All(r => r.EmployeeId != row.EmployeeId))
        {
            return null;
        }

        return await BuildAsync(team, row, ct);
    }

    /// <summary>
    /// The detail for a request whose owner is already known to be in <paramref name="team"/>. The decide use case
    /// reuses it to return the new state. The shared builder gives the same figures as the inbox, and the person and
    /// leave type names for every status. Balance and overlap are kept only while the request is pending.
    /// </summary>
    internal async Task<ApprovalDetail> BuildAsync(
        IReadOnlyList<ReportInfo> team, RequestDetailRow row, CancellationToken ct)
    {
        var pendingRow = new PendingRequestRow(
            row.RequestId, row.EmployeeId, row.LeaveTypeId, row.StartDate, row.EndDate,
            row.HalfDay, row.DaysCount, row.SubmittedAtUtc);
        var item = (await builder.BuildAsync(team, [pendingRow], ct))[0];

        var isPending = row.Status == LeaveStatus.Pending;

        // A cancelled request was not decided by the manager, so it has no decision to show.
        DecisionInfo? decision = row.Status is LeaveStatus.Approved or LeaveStatus.Rejected
            && row.DecidedAtUtc is { } decidedAt
            ? new DecisionInfo(row.Status.ToString(), decidedAt, row.DecisionComment)
            : null;

        return new ApprovalDetail(
            item.RequestId,
            row.Status,
            item.Employee,
            item.LeaveType,
            item.StartDate,
            item.EndDate,
            item.HalfDay,
            item.DaysCount,
            item.SubmittedAtUtc,
            isPending ? item.Balance : null,
            isPending ? item.TeamOverlap : null,
            decision,
            isPending ? ["approve", "reject"] : []);
    }
}
