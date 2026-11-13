namespace LeaveManagement.Domain;

/// <summary>The first rule a request breaks. The Application layer turns each into a code and a message.</summary>
public enum LeaveRuleViolation
{
    DateRange,
    YearCross,
    DatePast,
    HalfDayInvalid,
    DaysNone,
    NoticeShort,
    MaxConsecutive,
}

/// <param name="Today">The person's own date today (their location's time zone). The Domain has no clock.</param>
/// <param name="AllowHalfDay">Whether the leave type allows half days.</param>
/// <param name="MinNoticeWorkingDays">Rule 7: from the leave type. 0 means no notice is needed.</param>
/// <param name="MaxConsecutiveDays">Rule 8: from the leave type. Null means no limit.</param>
/// <param name="Holidays">Holidays of the person's location, covering the days from today to the end date.</param>
/// <param name="Counted">The result of <see cref="WorkingDayCalculator.Count"/> for this range.</param>
public sealed record LeaveRuleInput(
    DateOnly Start,
    DateOnly End,
    HalfDay HalfDay,
    DateOnly Today,
    bool AllowHalfDay,
    int MinNoticeWorkingDays,
    int? MaxConsecutiveDays,
    IReadOnlySet<DateOnly> Holidays,
    WorkingDayResult Counted);

/// <summary>
/// The leave rules as pure functions: no clock, no database. Returns only the first failure, in the order
/// of contracts/leave-requests-api.md (the unknown leave type is checked before these).
/// The numbers come from the leave type, so an admin can change them.
/// </summary>
public static class LeaveRules
{
    /// <summary>
    /// The two checks that need only the dates, so a caller can run them before it reads holidays or counts
    /// days. They also bound the work: a range that passes is inside one calendar year, at most 366 days.
    /// </summary>
    public static LeaveRuleViolation? StructureViolation(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return LeaveRuleViolation.DateRange;
        }

        // A request never crosses a leave year (the employee submits two).
        if (start.Year != end.Year)
        {
            return LeaveRuleViolation.YearCross;
        }

        return null;
    }

    public static LeaveRuleViolation? FirstViolation(LeaveRuleInput input)
    {
        if (StructureViolation(input.Start, input.End) is { } structure)
        {
            return structure;
        }

        if (input.Start < input.Today)
        {
            return LeaveRuleViolation.DatePast;
        }

        // Rule 11: a half day is one working day, and the type must allow it. A half day on a
        // weekend or holiday counts no slot, so it is refused here and not as "no working days".
        if (input.HalfDay != HalfDay.None
            && (input.Start != input.End || !input.AllowHalfDay || input.Counted.Slots.Count == 0))
        {
            return LeaveRuleViolation.HalfDayInvalid;
        }

        if (input.Counted.Slots.Count == 0)
        {
            return LeaveRuleViolation.DaysNone;
        }

        // Rule 7: working days strictly between today and the start date.
        if (input.MinNoticeWorkingDays > 0
            && WorkingDaysBetween(input.Today, input.Start, input.Holidays, input.MinNoticeWorkingDays)
                < input.MinNoticeWorkingDays)
        {
            return LeaveRuleViolation.NoticeShort;
        }

        // Rule 8: calendar days from start to end, weekends and holidays inside the range included.
        if (input.MaxConsecutiveDays is { } max && input.End.DayNumber - input.Start.DayNumber + 1 > max)
        {
            return LeaveRuleViolation.MaxConsecutive;
        }

        return null;
    }

    /// <summary>
    /// Working days (Monday to Friday, not a holiday) strictly after <paramref name="after"/> and strictly
    /// before <paramref name="before"/>. Stops counting at <paramref name="stopAt"/>, because the caller only
    /// needs to know whether the notice is long enough.
    /// </summary>
    public static int WorkingDaysBetween(
        DateOnly after, DateOnly before, IReadOnlySet<DateOnly> holidays, int stopAt)
    {
        var count = 0;
        for (var date = after.AddDays(1); date < before && count < stopAt; date = date.AddDays(1))
        {
            if (!WorkingDayCalculator.IsWeekend(date) && !holidays.Contains(date))
            {
                count++;
            }
        }

        return count;
    }
}
