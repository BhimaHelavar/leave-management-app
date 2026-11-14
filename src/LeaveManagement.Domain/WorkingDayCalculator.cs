namespace LeaveManagement.Domain;

/// <summary>One half-day slot of a working day. A full day has an AM and a PM slot.</summary>
public readonly record struct LeaveSlot(DateOnly Date, Slot Slot);

/// <param name="Slots">Every counted half-day slot, in date order. These become the request's day rows.</param>
/// <param name="WorkingDays">The distinct counted dates, in date order.</param>
/// <param name="SkippedHolidays">Weekday holidays inside the range that were not counted.</param>
public sealed record WorkingDayResult(
    IReadOnlyList<LeaveSlot> Slots,
    IReadOnlyList<DateOnly> WorkingDays,
    IReadOnlyList<DateOnly> SkippedHolidays)
{
    /// <summary>Number of slots times 0.5, so it can never disagree with the saved day rows.</summary>
    public decimal DaysCount => Slots.Count * 0.5m;
}

/// <summary>
/// Counts the working days of a leave range. Pure: no clock, no database. Monday to Friday only;
/// a weekend or holiday never counts, even between two leave days (sandwich rule off, rule 10).
/// </summary>
public static class WorkingDayCalculator
{
    public static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// A half day only applies when start and end are the same day; for a longer range it is ignored here
    /// and the rule check refuses the request. An end before the start counts nothing.
    /// </summary>
    public static WorkingDayResult Count(
        DateOnly start, DateOnly end, HalfDay halfDay, IReadOnlySet<DateOnly> holidays)
    {
        var slots = new List<LeaveSlot>();
        var days = new List<DateOnly>();
        var skipped = new List<DateOnly>();

        var singleHalfDay = halfDay != HalfDay.None && start == end;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (IsWeekend(date))
            {
                continue;
            }

            if (holidays.Contains(date))
            {
                skipped.Add(date);
                continue;
            }

            days.Add(date);

            if (singleHalfDay)
            {
                slots.Add(new LeaveSlot(date, halfDay == HalfDay.FirstHalf ? Slot.AM : Slot.PM));
            }
            else
            {
                slots.Add(new LeaveSlot(date, Slot.AM));
                slots.Add(new LeaveSlot(date, Slot.PM));
            }
        }

        return new WorkingDayResult(slots, days, skipped);
    }
}
