using LeaveManagement.Domain;

namespace LeaveManagement.Application.Leave;

/// <summary>The policy numbers of one leave type. They are stored per type so an admin can change them.</summary>
public sealed record LeaveTypeRules(
    int Id,
    bool IsActive,
    AccrualMode AccrualMode,
    bool AllowHalfDay,
    int MinNoticeWorkingDays,
    int? MaxConsecutiveDays)
{
    /// <summary>A type with no accrual (Unpaid) has no balance to deduct from.</summary>
    public bool HasBalance => AccrualMode != AccrualMode.None;
}

public interface ILeaveTypeReader
{
    /// <summary>Null when there is no such leave type.</summary>
    Task<LeaveTypeRules?> GetAsync(int leaveTypeId, CancellationToken ct);
}
