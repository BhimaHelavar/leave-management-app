using LeaveManagement.Domain;

namespace LeaveManagement.Application.Approvals;

/// <param name="HasBalance">False for a type with no balance (Unpaid): a rejection then restores nothing.</param>
/// <param name="Comment">The cleaned comment from <see cref="LeaveDecision.CheckComment"/>, or null.</param>
public sealed record LeaveDecisionCommand(
    int RequestId,
    int OwnerEmployeeId,
    int LeaveTypeId,
    int LeaveYear,
    decimal Days,
    DecisionOutcome Outcome,
    string? Comment,
    int DecidedByEmployeeId,
    bool HasBalance);

/// <summary>Success, or a refusal. Nothing changed when it is a refusal.</summary>
public sealed record DecisionResult(bool Succeeded, string? RefusalCode)
{
    public static DecisionResult Success() => new(true, null);

    public static DecisionResult NotPending() => new(false, DecisionRefusalCodes.NotPending);
}

public static class DecisionRefusalCodes
{
    public const string NotPending = "request.not_pending";
}

/// <summary>
/// The named cross-module store allowed by constitution Principle III (spec FR-018). One decision writes the request,
/// its day rows, the balance and ledger, the audit row and the outbox row in one database transaction, because the
/// rule needs that atomicity. No other class may write across modules.
/// </summary>
public interface ILeaveDecisionStore
{
    /// <summary>
    /// Approves or rejects a request only if it is still pending (a guarded update, so a lost race gives
    /// <c>request.not_pending</c>). A refusal leaves every table as it was.
    /// </summary>
    Task<DecisionResult> DecideAsync(LeaveDecisionCommand command, CancellationToken ct);
}
