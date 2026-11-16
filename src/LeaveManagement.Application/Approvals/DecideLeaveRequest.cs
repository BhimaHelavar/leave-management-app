using LeaveManagement.Application.Leave;
using LeaveManagement.Domain;

namespace LeaveManagement.Application.Approvals;

/// <summary>What happened, so the endpoint maps it to a status code and holds no rule itself.</summary>
public enum DecideStatus
{
    /// <summary>Decided. 200 with the new detail.</summary>
    Ok,

    /// <summary>Comment over 500 characters. 400.</summary>
    CommentTooLong,

    /// <summary>A rejection without a comment. 422 with <c>comment.required</c>.</summary>
    CommentRequired,

    /// <summary>No such request, or its owner is not a direct report. 404, the same for both.</summary>
    NotFound,

    /// <summary>Already decided or cancelled, or a lost race. 409 with <c>request.not_pending</c>.</summary>
    NotPending,
}

/// <param name="Detail">Set only when <paramref name="Status"/> is Ok.</param>
/// <param name="Error">Code and message for CommentRequired and NotPending; null otherwise.</param>
public sealed record DecideResult(DecideStatus Status, ApprovalDetail? Detail = null, LeaveRuleError? Error = null)
{
    public static DecideResult Ok(ApprovalDetail detail) => new(DecideStatus.Ok, detail);

    public static DecideResult CommentTooLong() => new(DecideStatus.CommentTooLong);

    public static DecideResult CommentRequired() => new(DecideStatus.CommentRequired, null, LeaveRuleErrors.CommentRequired());

    public static DecideResult NotFound() => new(DecideStatus.NotFound);

    public static DecideResult NotPending() => new(DecideStatus.NotPending, null, LeaveRuleErrors.RequestNotPending());
}

/// <summary>
/// Use case: approve or reject a direct report's pending request (US-07). The caller is the signed-in manager and is
/// never read from the body. The write itself is one guarded transaction in <see cref="ILeaveDecisionStore"/>. The
/// comment is never logged (security rule 12).
/// </summary>
public sealed class DecideLeaveRequest(
    IDirectReportReader reports,
    IApprovalRequestReader requests,
    ILeaveTypeReader leaveTypes,
    ILeaveDecisionStore store,
    GetApprovalDetail detail)
{
    public async Task<DecideResult> ExecuteAsync(
        int managerId, int requestId, DecisionOutcome outcome, string? comment, CancellationToken ct)
    {
        // 1. The comment rule: too long first, then required.
        var check = LeaveDecision.CheckComment(outcome, comment);
        if (check.Problem == DecisionCommentProblem.TooLong)
        {
            return DecideResult.CommentTooLong();
        }

        if (check.Problem == DecisionCommentProblem.Missing)
        {
            return DecideResult.CommentRequired();
        }

        // 2. The request must exist and belong to a direct report. Both failures look the same (security rule 4).
        var team = await reports.GetAsync(managerId, ct);
        var row = await requests.GetByIdAsync(requestId, ct);
        if (row is null || team.All(r => r.EmployeeId != row.EmployeeId))
        {
            return DecideResult.NotFound();
        }

        // 3. Only a pending request can be decided.
        if (row.Status != LeaveStatus.Pending)
        {
            return DecideResult.NotPending();
        }

        // 4. A type with no balance (Unpaid) restores nothing on reject. A request always points at a type that
        // exists (types are never deleted), so a missing one is a fault. Guessing "no balance" here would lose
        // the days silently on a rejection.
        var type = await leaveTypes.GetAsync(row.LeaveTypeId, ct)
            ?? throw new InvalidOperationException(
                $"Leave type {row.LeaveTypeId} of request {row.RequestId} was not found.");
        var hasBalance = type.HasBalance;

        // 5. The saved days snapshot is used, never a recalculation (the request never crosses a leave year).
        var result = await store.DecideAsync(
            new LeaveDecisionCommand(
                row.RequestId,
                row.EmployeeId,
                row.LeaveTypeId,
                row.StartDate.Year,
                row.DaysCount,
                outcome,
                check.Comment,
                managerId,
                hasBalance),
            ct);

        if (!result.Succeeded)
        {
            return DecideResult.NotPending();
        }

        // 6. Return the new state, read again so it shows what was saved.
        var saved = await requests.GetByIdAsync(requestId, ct);
        return saved is null
            ? DecideResult.NotFound()
            : DecideResult.Ok(await detail.BuildAsync(team, saved, ct));
    }
}
