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

// Shapes of GET /api/approvals/{id} and the approve and reject calls (contracts/decision-api.md).

/// <param name="Balance">Null once decided, or for a type with no balance.</param>
/// <param name="TeamOverlap">Null once decided.</param>
/// <param name="Decision">Null while pending.</param>
/// <param name="AllowedActions">"approve" and "reject" only while pending; empty otherwise. Computed on the server.</param>
public sealed record ApprovalDetail(
    int RequestId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LeaveStatus Status,
    PersonRef Employee,
    LeaveTypeRef LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] HalfDay HalfDay,
    decimal DaysCount,
    DateTime SubmittedAtUtc,
    BalanceBeforeAfter? Balance,
    TeamOverlapInfo? TeamOverlap,
    DecisionInfo? Decision,
    IReadOnlyList<string> AllowedActions);

/// <param name="Outcome">"Approved" or "Rejected".</param>
/// <param name="Comment">The cleaned comment. May be null for an approval.</param>
public sealed record DecisionInfo(string Outcome, DateTime DecidedAtUtc, string? Comment);

/// <summary>Body of approve and reject. The comment is optional on the wire; the rule is checked in the use case.</summary>
public sealed record DecideRequestBody(string? Comment);

public sealed record PersonRef(int Id, string Name);

public sealed record LeaveTypeRef(int Id, string Code, string Name);

/// <param name="Before">After plus this request's days.</param>
/// <param name="After">The requester's available days now, already net of this request.</param>
public sealed record BalanceBeforeAfter(decimal Before, decimal After);

/// <param name="TeamSize">The active direct reports, the requester included.</param>
/// <param name="AwayIfApproved">The requester plus the others who are away on the same days.</param>
public sealed record TeamOverlapInfo(int TeamSize, int AwayIfApproved, IReadOnlyList<OverlapPerson> Others);

public sealed record OverlapPerson(int EmployeeId, string Name, IReadOnlyList<DateOnly> Dates);
