using LeaveManagement.Application.Balances;
using LeaveManagement.Domain;

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
    IBalanceReader balances,
    TimeProvider time)
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

        var teamIds = team.Select(r => r.EmployeeId).ToList();
        var names = team.ToDictionary(r => r.EmployeeId, r => r.FullName);

        var pending = await requests.GetPendingPageAsync(teamIds, page, pageSize, ct);
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        // One balance read per person and leave year on this page, not one per request.
        var ledgers = new Dictionary<(int EmployeeId, short Year), IReadOnlyList<LeaveTypeLedger>>();
        foreach (var key in pending.Rows.Select(r => (r.EmployeeId, Year: (short)r.StartDate.Year)).Distinct())
        {
            ledgers[key] = await balances.GetLedgerAsync(key.EmployeeId, key.Year, today, ct);
        }

        var items = new List<ApprovalItem>(pending.Rows.Count);
        foreach (var row in pending.Rows)
        {
            var ledger = ledgers[(row.EmployeeId, (short)row.StartDate.Year)]
                .FirstOrDefault(l => l.LeaveTypeId == row.LeaveTypeId);

            // The ledger lists only active types. A request for a type that is no longer there still shows,
            // with a plain name and no balance.
            var type = ledger is null
                ? new LeaveTypeRef(row.LeaveTypeId, string.Empty, $"Leave type {row.LeaveTypeId}")
                : new LeaveTypeRef(ledger.LeaveTypeId, ledger.Code, ledger.Name);

            // Days are deducted at submit, so Available is already net of this request: that is the "after"
            // figure, and "before" adds this request's days back. Unpaid has no balance.
            BalanceBeforeAfter? balance = null;
            if (ledger is not null && !(ledger.Available is null && ledger.AccrualMode == AccrualMode.None))
            {
                var after = ledger.Available ?? 0m;
                balance = new BalanceBeforeAfter(after + row.DaysCount, after);
            }

            // Interim until the overlap is wired in (tasks T010): no one else is shown, the requester counts.
            var overlap = new TeamOverlapInfo(team.Count, 1, []);

            items.Add(new ApprovalItem(
                row.RequestId,
                new PersonRef(row.EmployeeId, names[row.EmployeeId]),
                type,
                row.StartDate,
                row.EndDate,
                row.HalfDay,
                row.DaysCount,
                row.SubmittedAtUtc,
                balance,
                overlap));
        }

        return new ManagerInboxResponse(items, page, pageSize, pending.TotalCount);
    }
}
