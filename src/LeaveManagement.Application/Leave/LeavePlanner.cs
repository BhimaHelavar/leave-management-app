using LeaveManagement.Domain;

namespace LeaveManagement.Application.Leave;

/// <summary>A request that passed every rule that needs no balance or overlap lookup.</summary>
public sealed record LeavePlan(WorkingDayResult Counted, LeaveTypeRules Type);

/// <param name="UnknownPerson">The person is missing or inactive. The endpoint answers 401.</param>
public sealed record PlanOutcome(bool UnknownPerson, LeavePlan? Plan, LeaveRuleError? Error)
{
    public static PlanOutcome Unknown() => new(true, null, null);

    public static PlanOutcome Refused(LeaveRuleError error) => new(false, null, error);

    public static PlanOutcome Planned(LeavePlan plan) => new(false, plan, null);
}

/// <summary>
/// The steps that Apply and Preview share: read the person, the leave type and the holidays, count the
/// working days, and check the rules. One place, so the preview on the form can never disagree with
/// what a submit saves (FR-018). Balance and overlap are not checked here; only a submit decides those.
/// </summary>
public sealed class LeavePlanner(
    IApplicantReader applicants,
    ILeaveTypeReader leaveTypes,
    IHolidayReader holidays,
    TimeProvider time)
{
    public async Task<PlanOutcome> PlanAsync(
        int employeeId,
        int leaveTypeId,
        DateOnly start,
        DateOnly end,
        HalfDay halfDay,
        CancellationToken ct)
    {
        var applicant = await applicants.GetAsync(employeeId, ct);
        if (applicant is not { IsActive: true })
        {
            return PlanOutcome.Unknown();
        }

        var type = await leaveTypes.GetAsync(leaveTypeId, ct);
        if (type is not { IsActive: true })
        {
            return PlanOutcome.Refused(LeaveRuleErrors.TypeUnknown());
        }

        // Refuse a backwards or year-crossing range before any holiday read or day count, so a huge range
        // cannot make the server do work only to refuse it afterwards.
        if (LeaveRules.StructureViolation(start, end) is { } structure)
        {
            return PlanOutcome.Refused(ToError(structure, type));
        }

        var today = LocalToday.For(time, applicant.TimeZoneId);

        // Holidays from today (for the notice count) to the end date (for the day count).
        var holidayDates = await holidays.GetHolidaysAsync(
            applicant.LocationId, start < today ? start : today, end, ct);

        var counted = WorkingDayCalculator.Count(start, end, halfDay, holidayDates);

        var violation = LeaveRules.FirstViolation(new LeaveRuleInput(
            start,
            end,
            halfDay,
            today,
            type.AllowHalfDay,
            type.MinNoticeWorkingDays,
            type.MaxConsecutiveDays,
            holidayDates,
            counted));

        return violation is null
            ? PlanOutcome.Planned(new LeavePlan(counted, type))
            : PlanOutcome.Refused(ToError(violation.Value, type));
    }

    private static LeaveRuleError ToError(LeaveRuleViolation violation, LeaveTypeRules type) => violation switch
    {
        LeaveRuleViolation.DateRange => LeaveRuleErrors.DateRange(),
        LeaveRuleViolation.YearCross => LeaveRuleErrors.YearCross(),
        LeaveRuleViolation.DatePast => LeaveRuleErrors.DatePast(),
        LeaveRuleViolation.HalfDayInvalid => LeaveRuleErrors.HalfDayInvalid(),
        LeaveRuleViolation.DaysNone => LeaveRuleErrors.DaysNone(),
        LeaveRuleViolation.NoticeShort => LeaveRuleErrors.NoticeShort(type.MinNoticeWorkingDays),
        LeaveRuleViolation.MaxConsecutive => LeaveRuleErrors.MaxConsecutive(type.MaxConsecutiveDays ?? 0),
        _ => throw new ArgumentOutOfRangeException(nameof(violation), violation, "Unmapped rule violation."),
    };
}

/// <summary>
/// "Today" for a person: the instant from <see cref="TimeProvider"/> shown in their location's time zone.
/// Near midnight UTC the date in Bengaluru is already the next day, so a UTC date would be wrong for them.
/// </summary>
internal static class LocalToday
{
    public static DateOnly For(TimeProvider time, string timeZoneId)
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Never fall back to UTC silently: that would give the wrong "today" for some people.
            // The exception handler logs this and answers 500 ProblemDetails with no detail.
            throw new InvalidOperationException(
                $"The time zone '{timeZoneId}' of the person's location cannot be resolved on this host.", ex);
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), zone).DateTime);
    }
}
