using System.Text.Json.Serialization;
using LeaveManagement.Domain;

namespace LeaveManagement.Application.Leave;

// The request has no employee id, status or day count: the server decides those (over-posting closed).

/// <param name="Reason">Optional, up to 500 characters. Stored on the request, never copied into the audit row.</param>
public sealed record ApplyLeaveRequest(
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] HalfDay HalfDay,
    string? Reason);

/// <param name="DaysCount">decimal(4,1): multiples of 0.5, greater than 0.</param>
public sealed record LeaveRequestResponse(
    int Id,
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] HalfDay HalfDay,
    decimal DaysCount,
    string Status,
    short LeaveYear);

public sealed record PreviewResponse(
    decimal DaysCount,
    IReadOnlyList<DateOnly> WorkingDays,
    IReadOnlyList<DateOnly> SkippedHolidays);

/// <summary>Either the saved request or the reason it was refused. Exactly one is set.</summary>
public sealed record ApplyResult(LeaveRequestResponse? Response, LeaveRuleError? Error)
{
    public static ApplyResult Ok(LeaveRequestResponse response) => new(response, null);

    public static ApplyResult Fail(LeaveRuleError error) => new(null, error);
}
