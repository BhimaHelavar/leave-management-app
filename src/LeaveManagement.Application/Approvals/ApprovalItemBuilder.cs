using LeaveManagement.Application.Balances;
using LeaveManagement.Domain;

namespace LeaveManagement.Application.Approvals;

/// <summary>
/// Builds the approval item (person, type, balance before and after, team overlap) from request rows. The inbox
/// and the request detail both use it, so the two screens cannot disagree. It only reads. It takes a list, so a
/// page and a single request use the same code: pass one row for one request.
/// </summary>
public sealed class ApprovalItemBuilder(
    IApprovalRequestReader requests,
    IBalanceReader balances,
    TimeProvider time)
{
    /// <summary>
    /// Items for these rows, in the same order. <paramref name="team"/> is the manager's whole team (it sets the
    /// team size and supplies the names); every row's employee must be in it.
    /// </summary>
    public async Task<IReadOnlyList<ApprovalItem>> BuildAsync(
        IReadOnlyList<ReportInfo> team, IReadOnlyList<PendingRequestRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var teamIds = team.Select(r => r.EmployeeId).ToList();
        var names = team.ToDictionary(r => r.EmployeeId, r => r.FullName);
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        // One balance read per person and leave year in these rows, not one per request.
        var ledgers = new Dictionary<(int EmployeeId, short Year), IReadOnlyList<LeaveTypeLedger>>();
        foreach (var key in rows.Select(r => (r.EmployeeId, Year: (short)r.StartDate.Year)).Distinct())
        {
            ledgers[key] = await balances.GetLedgerAsync(key.EmployeeId, key.Year, today, ct);
        }

        // One read of the team's held slots for the dates in these rows, not one per request.
        var held = await requests.GetHeldSlotsAsync(
            teamIds, rows.Min(r => r.StartDate), rows.Max(r => r.EndDate), ct);

        var items = new List<ApprovalItem>(rows.Count);
        foreach (var row in rows)
        {
            var ledger = ledgers[(row.EmployeeId, (short)row.StartDate.Year)]
                .FirstOrDefault(l => l.LeaveTypeId == row.LeaveTypeId);

            // The ledger lists only active types. A request for a type that is no longer there still shows,
            // with a plain name and no balance.
            var type = ledger is null
                ? new LeaveTypeRef(row.LeaveTypeId, string.Empty, $"Leave type {row.LeaveTypeId}")
                : new LeaveTypeRef(ledger.LeaveTypeId, ledger.Code, ledger.Name);

            // Days are deducted at submit, so Available is already net of this request: that is the "after"
            // figure, and "before" adds this request's days back. No balance row (Unpaid, or no row for that
            // person and leave year) means no figures, not zero.
            BalanceBeforeAfter? balance = null;
            if (ledger?.Available is { } after)
            {
                balance = new BalanceBeforeAfter(after + row.DaysCount, after);
            }

            // Who else is away on the same slots, and how many would be away if this is approved (the requester counts).
            var shared = TeamOverlap.For(row.RequestId, row.EmployeeId, held);
            var overlap = new TeamOverlapInfo(
                team.Count,
                shared.AwayIfApproved,
                shared.Others.Select(o => new OverlapPerson(o.EmployeeId, names[o.EmployeeId], o.Dates)).ToList());

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

        return items;
    }
}
