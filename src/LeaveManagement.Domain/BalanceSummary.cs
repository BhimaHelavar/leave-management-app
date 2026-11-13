namespace LeaveManagement.Domain;

/// <summary>The four figures shown on a balance card. Accrued - Used - Pending = Available.</summary>
public readonly record struct BalanceFigures(decimal Accrued, decimal Used, decimal Pending, decimal Available);

/// <summary>
/// Derives the card figures from stored values (research R1). Nothing is stored twice, so the
/// figures cannot drift from the ledger. Days are deducted at submit, so Available is already
/// net of pending and approved days.
/// </summary>
public static class BalanceSummary
{
    /// <param name="available">The current LeaveBalance.Available.</param>
    /// <param name="held">Days held by pending or approved requests: -(Deduction + Restore ledger entries).</param>
    /// <param name="pending">Days of requests still awaiting a decision.</param>
    public static BalanceFigures Derive(decimal available, decimal held, decimal pending) =>
        new(Accrued: available + held, Used: held - pending, Pending: pending, Available: available);

    /// <summary>
    /// The part of the available days that is carry-forward and will lapse (research R5).
    /// Assumption, to be confirmed by the owner: carry-forward days are used first, so what is left is
    /// what was carried minus everything taken out this year, never more than what is available.
    /// </summary>
    /// <param name="carryForwardGranted">Carry-forward entries that have not expired yet.</param>
    public static decimal CarryForwardRemaining(decimal available, decimal held, decimal carryForwardGranted) =>
        Math.Min(available, Math.Max(0m, carryForwardGranted - held));
}
