namespace LeaveManagement.Application.Balances;

/// <summary>Port for the leave module. Reads leave.* tables only.</summary>
public interface IPendingLeaveReader
{
    /// <summary>Pending days per leave type id for one person and year. Types with none are absent.</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetPendingDaysAsync(
        int employeeId, short leaveYear, CancellationToken ct);
}
