using LeaveManagement.Domain;

namespace LeaveManagement.Application.Balances;

/// <summary>
/// Use case: the signed-in person's balance per leave type for the current leave year.
/// It joins the balances module and the leave module through two ports, so neither reads the
/// other's tables (structure.md). All figures are computed here, never on the screen (FR-010).
/// </summary>
public sealed class GetMyBalances(
    IBalanceReader balances, IPendingLeaveReader pending, TimeProvider time)
{
    public async Task<MyBalancesResponse> ExecuteAsync(int employeeId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var year = (short)today.Year;

        var ledgers = await balances.GetLedgerAsync(employeeId, year, today, ct);
        var pendingByType = await pending.GetPendingDaysAsync(employeeId, year, ct);

        var cards = new List<LeaveTypeBalance>(ledgers.Count);
        foreach (var ledger in ledgers)
        {
            // Types with no balance (Unpaid) carry no numbers at all.
            if (ledger.Available is null && ledger.AccrualMode == AccrualMode.None)
            {
                cards.Add(new LeaveTypeBalance(
                    ledger.LeaveTypeId, ledger.Code, ledger.Name,
                    HasBalance: false, null, null, null, null, null, null));
                continue;
            }

            // A type that accrues but has no row yet shows zeros, not an error.
            var figures = BalanceSummary.Derive(
                ledger.Available ?? 0m,
                ledger.Held,
                pendingByType.GetValueOrDefault(ledger.LeaveTypeId));

            // Only show the carry-forward part while some of it is left; otherwise both fields are null.
            var carryForward = BalanceSummary.CarryForwardRemaining(
                figures.Available, ledger.Held, ledger.CarryForwardGranted);

            cards.Add(new LeaveTypeBalance(
                ledger.LeaveTypeId, ledger.Code, ledger.Name,
                HasBalance: true,
                figures.Accrued, figures.Used, figures.Pending, figures.Available,
                CarryForwardDays: carryForward > 0 ? carryForward : null,
                CarryForwardExpiresOn: carryForward > 0 ? ledger.CarryForwardExpiresOn : null));
        }

        return new MyBalancesResponse(year, cards);
    }
}
