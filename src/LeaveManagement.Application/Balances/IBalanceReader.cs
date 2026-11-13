using LeaveManagement.Domain;

namespace LeaveManagement.Application.Balances;

/// <summary>
/// What the balances module knows about one active leave type for one person and year.
/// </summary>
/// <param name="Available">Null when the person has no balance row for the type.</param>
/// <param name="Held">Days taken out for requests that are pending or approved: -(Deduction + Restore entries).</param>
/// <param name="CarryForwardGranted">Sum of CarryForward entries that expire on or after today.</param>
/// <param name="CarryForwardExpiresOn">Earliest expiry among those entries; null when there are none.</param>
public sealed record LeaveTypeLedger(
    int LeaveTypeId,
    string Code,
    string Name,
    AccrualMode AccrualMode,
    decimal? Available,
    decimal Held,
    decimal CarryForwardGranted,
    DateOnly? CarryForwardExpiresOn);

/// <summary>Port for the balances module. Reads balances.* tables only.</summary>
public interface IBalanceReader
{
    Task<IReadOnlyList<LeaveTypeLedger>> GetLedgerAsync(
        int employeeId, short leaveYear, DateOnly today, CancellationToken ct);
}
