namespace LeaveManagement.Application.Leave;

/// <summary>Why a request was refused. The code is stable for the screen; the message is plain words.</summary>
public sealed record LeaveRuleError(string Code, string Message);

/// <summary>The codes in contracts/leave-requests-api.md.</summary>
public static class LeaveRuleCodes
{
    public const string DateRange = "date.range";
    public const string DatePast = "date.past";
    public const string YearCross = "year.cross";
    public const string DaysNone = "days.none";
    public const string HalfDayInvalid = "halfday.invalid";
    public const string TypeUnknown = "type.unknown";
    public const string NoticeShort = "notice.short";
    public const string MaxConsecutive = "max.consecutive";
    public const string Overlap = "leave.overlap";
    public const string BalanceInsufficient = "balance.insufficient";
    public const string IdempotencyMismatch = "idempotency.mismatch";
}

/// <summary>Builds each refusal with the exact wording from the contract. {n} comes from the leave type.</summary>
public static class LeaveRuleErrors
{
    public static LeaveRuleError DateRange() =>
        new(LeaveRuleCodes.DateRange, "The end date cannot be before the start date.");

    public static LeaveRuleError DatePast() =>
        new(LeaveRuleCodes.DatePast, "The start date cannot be in the past.");

    public static LeaveRuleError YearCross() =>
        new(LeaveRuleCodes.YearCross, "A request cannot cross two leave years. Submit one request for each year.");

    public static LeaveRuleError DaysNone() =>
        new(LeaveRuleCodes.DaysNone, "The dates you chose have no working days. Weekends and holidays are not counted.");

    public static LeaveRuleError HalfDayInvalid() =>
        new(LeaveRuleCodes.HalfDayInvalid, "A half day must be a single working day, and this leave type must allow half days.");

    public static LeaveRuleError TypeUnknown() =>
        new(LeaveRuleCodes.TypeUnknown, "This leave type is not available.");

    public static LeaveRuleError NoticeShort(int workingDays) =>
        new(LeaveRuleCodes.NoticeShort, $"This leave type needs at least {workingDays} working days' notice.");

    public static LeaveRuleError MaxConsecutive(int days) =>
        new(LeaveRuleCodes.MaxConsecutive, $"This leave type allows at most {days} days in a row. Longer leave needs HR.");

    public static LeaveRuleError Overlap() =>
        new(LeaveRuleCodes.Overlap, "You already have a pending or approved leave request on one or more of these days.");

    public static LeaveRuleError BalanceInsufficient() =>
        new(LeaveRuleCodes.BalanceInsufficient, "You do not have enough balance for these days.");

    public static LeaveRuleError IdempotencyMismatch() =>
        new(LeaveRuleCodes.IdempotencyMismatch, "This request key was already used for a different request.");
}
