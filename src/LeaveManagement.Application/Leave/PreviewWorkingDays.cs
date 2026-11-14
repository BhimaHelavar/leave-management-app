using LeaveManagement.Domain;

namespace LeaveManagement.Application.Leave;

/// <summary>Either the preview or the reason the dates are not acceptable. Exactly one is set.</summary>
public sealed record PreviewResult(PreviewResponse? Response, LeaveRuleError? Error)
{
    public static PreviewResult Ok(PreviewResponse response) => new(response, null);

    public static PreviewResult Fail(LeaveRuleError error) => new(null, error);
}

/// <summary>
/// Use case: the working-day count for the form, before the person submits. It runs the same
/// <see cref="LeavePlanner"/> as a submit, so the number is the one a submit would save. It writes nothing
/// (no request, no audit row). It does not check balance or overlap; only a submit decides those.
/// </summary>
public sealed class PreviewWorkingDays(LeavePlanner planner)
{
    /// <returns>The preview or the refusal. Null when the person is unknown or inactive (the endpoint answers 401).</returns>
    public async Task<PreviewResult?> ExecuteAsync(
        int employeeId,
        int leaveTypeId,
        DateOnly start,
        DateOnly end,
        HalfDay halfDay,
        CancellationToken ct)
    {
        var outcome = await planner.PlanAsync(employeeId, leaveTypeId, start, end, halfDay, ct);

        if (outcome.UnknownPerson)
        {
            return null;
        }

        if (outcome.Error is { } error)
        {
            return PreviewResult.Fail(error);
        }

        var counted = outcome.Plan!.Counted;
        return PreviewResult.Ok(new PreviewResponse(counted.DaysCount, counted.WorkingDays, counted.SkippedHolidays));
    }
}
