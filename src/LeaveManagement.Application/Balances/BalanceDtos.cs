namespace LeaveManagement.Application.Balances;

/// <summary>Response of GET /api/me/balances. Shape fixed by contracts/balances-api.md.</summary>
public sealed record MyBalancesResponse(short LeaveYear, IReadOnlyList<LeaveTypeBalance> Balances);

/// <summary>
/// One leave type for one person. When <see cref="HasBalance"/> is false (for example Unpaid),
/// every number is null.
/// </summary>
public sealed record LeaveTypeBalance(
    int LeaveTypeId,
    string Code,
    string Name,
    bool HasBalance,
    decimal? Accrued,
    decimal? Used,
    decimal? Pending,
    decimal? Available,
    decimal? CarryForwardDays,
    DateOnly? CarryForwardExpiresOn);
