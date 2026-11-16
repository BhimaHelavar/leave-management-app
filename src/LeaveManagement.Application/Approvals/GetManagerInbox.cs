namespace LeaveManagement.Application.Approvals;

/// <summary>
/// Use case: the pending requests of a manager's direct reports (US-06). It joins three modules through three
/// ports, so none reads another's tables (structure.md): people for the team, leave for the requests, balances
/// for the figures. Everything shown is computed here, never on the screen. It only reads: no request, balance or
/// audit row changes (FR-013).
/// </summary>
public sealed class GetManagerInbox(
    IDirectReportReader reports,
    IApprovalRequestReader requests,
    ApprovalItemBuilder builder)
{
    public async Task<ManagerInboxResponse> ExecuteAsync(
        int managerId, int page, int pageSize, CancellationToken ct)
    {
        // The team is looked up now, from the database, never taken from the token (security rule 7).
        var team = await reports.GetAsync(managerId, ct);
        if (team.Count == 0)
        {
            return new ManagerInboxResponse([], page, pageSize, 0);
        }

        var pending = await requests.GetPendingPageAsync(
            team.Select(r => r.EmployeeId).ToList(), page, pageSize, ct);
        var items = await builder.BuildAsync(team, pending.Rows, ct);

        return new ManagerInboxResponse(items, page, pageSize, pending.TotalCount);
    }
}
