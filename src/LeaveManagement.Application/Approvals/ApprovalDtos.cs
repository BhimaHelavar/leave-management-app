using System.Text.Json.Serialization;
using LeaveManagement.Domain;

namespace LeaveManagement.Application.Approvals;

// Shapes of GET /api/approvals (contracts/manager-inbox-api.md). They carry no leave reason, no email and no
// balance except the requester's: the manager sees only what is needed to decide (security rule 12).

public sealed record ManagerInboxResponse(
    IReadOnlyList<ApprovalItem> Items, int Page, int PageSize, int TotalCount);

/// <param name="DaysCount">The snapshot saved at submit, never recalculated. 0.5 for a half day.</param>
/// <param name="Balance">Null for a leave type with no balance (Unpaid).</param>
public sealed record ApprovalItem(
    int RequestId,
    PersonRef Employee,
    LeaveTypeRef LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] HalfDay HalfDay,
    decimal DaysCount,
    DateTime SubmittedAtUtc,
    BalanceBeforeAfter? Balance,
    TeamOverlapInfo TeamOverlap);

public sealed record PersonRef(int Id, string Name);

public sealed record LeaveTypeRef(int Id, string Code, string Name);

/// <param name="Before">After plus this request's days.</param>
/// <param name="After">The requester's available days now, already net of this request.</param>
public sealed record BalanceBeforeAfter(decimal Before, decimal After);

/// <param name="TeamSize">The active direct reports, the requester included.</param>
/// <param name="AwayIfApproved">The requester plus the others who are away on the same days.</param>
public sealed record TeamOverlapInfo(int TeamSize, int AwayIfApproved, IReadOnlyList<OverlapPerson> Others);

public sealed record OverlapPerson(int EmployeeId, string Name, IReadOnlyList<DateOnly> Dates);
