namespace LeaveManagement.Domain;

public enum DecisionOutcome
{
    Approve,
    Reject,
}

/// <summary>Why a decision comment was refused. Checked in this order: too long, then missing.</summary>
public enum DecisionCommentProblem
{
    TooLong,
    Missing,
}

/// <param name="Comment">The cleaned comment, or null when none was given. Null whenever <see cref="Problem"/> is set.</param>
/// <param name="Problem">Null when the comment is acceptable.</param>
public readonly record struct DecisionCommentResult(string? Comment, DecisionCommentProblem? Problem)
{
    public bool IsValid => Problem is null;
}

/// <summary>Rules for the comment on an approve or reject decision (spec FR on comments, research R7).</summary>
public static class LeaveDecision
{
    /// <summary>Matches the 500-character column on the request.</summary>
    public const int MaxCommentLength = 500;

    public static DecisionCommentResult CheckComment(DecisionOutcome outcome, string? rawComment)
    {
        var comment = string.IsNullOrWhiteSpace(rawComment) ? null : rawComment.Trim();

        if (comment is { Length: > MaxCommentLength })
        {
            return new DecisionCommentResult(null, DecisionCommentProblem.TooLong);
        }

        if (comment is null && outcome == DecisionOutcome.Reject)
        {
            return new DecisionCommentResult(null, DecisionCommentProblem.Missing);
        }

        return new DecisionCommentResult(comment, null);
    }
}
