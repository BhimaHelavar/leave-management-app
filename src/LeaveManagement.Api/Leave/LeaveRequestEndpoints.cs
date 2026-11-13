using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Features;
using LeaveManagement.Application.Leave;
using LeaveManagement.Domain;

namespace LeaveManagement.Api.Leave;

public static partial class LeaveRequestEndpoints
{
    private const int ReasonMaxLength = 500;
    private const long MaxBodyBytes = 4096;

    // Dates far outside this window are a mistake or an attack, not a leave request.
    private static readonly DateOnly EarliestDate = new(2000, 1, 1);
    private static readonly DateOnly LatestDate = new(2100, 12, 31);

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex KeyPattern();

    /// <summary>
    /// Leave requests of the caller. The person comes from the token (sub), never from the URL or body, so
    /// there is no employee id to probe or to over-post (security rules 4 and 8). No role policy: everyone is
    /// an Employee, and the fallback policy already requires a signed-in person.
    /// </summary>
    public static IEndpointRouteBuilder MapLeaveRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/me/leave-requests", async (
                ClaimsPrincipal user,
                HttpRequest http,
                ApplyForLeave useCase,
                CancellationToken ct) =>
            {
                if (!int.TryParse(user.FindFirstValue("sub"), out var employeeId))
                {
                    return Unauthorized();
                }

                var key = http.Headers["Idempotency-Key"].ToString();
                if (!KeyPattern().IsMatch(key))
                {
                    return BadRequest(
                        "The Idempotency-Key header is required: 1 to 64 letters, digits, '-' or '_'.");
                }

                // Read the body here so a malformed one is a 400 in every environment. Left to the framework,
                // Development throws and the exception handler turns it into a 500.
                var body = await ReadBodyAsync(http, ct);
                if (body is null)
                {
                    return BadRequest("The request body must be valid JSON for a leave request.");
                }

                var invalid = Validate(body);
                if (invalid is not null)
                {
                    return BadRequest(invalid);
                }

                var result = await useCase.ExecuteAsync(employeeId, key, body, ct);
                if (result is null)
                {
                    return Unauthorized();
                }

                if (result.Error is { } error)
                {
                    return Refused(error);
                }

                return Results.Json(result.Response, statusCode: StatusCodes.Status201Created);
            })
            .WithName("ApplyForLeave");

        // The count for the form. Same planner as a submit, writes nothing, no audit row.
        app.MapGet("/api/me/leave-requests/preview", async (
                ClaimsPrincipal user,
                HttpRequest http,
                PreviewWorkingDays useCase,
                CancellationToken ct) =>
            {
                if (!int.TryParse(user.FindFirstValue("sub"), out var employeeId))
                {
                    return Unauthorized();
                }

                // Read the query here so a malformed one is a 400 in every environment (see ReadBodyAsync).
                var query = ReadPreviewQuery(http.Query);
                if (query is null)
                {
                    return BadRequest(
                        "leaveTypeId, startDate and endDate (yyyy-MM-dd) are required; halfDay is optional.");
                }

                var invalid = Validate(query);
                if (invalid is not null)
                {
                    return BadRequest(invalid);
                }

                var result = await useCase.ExecuteAsync(
                    employeeId, query.LeaveTypeId, query.StartDate, query.EndDate, query.HalfDay, ct);
                if (result is null)
                {
                    return Unauthorized();
                }

                return result.Error is { } error ? Refused(error) : Results.Ok(result.Response);
            })
            .WithName("PreviewLeaveRequest");

        return app;
    }

    // The query has the same fields as the apply body (no reason), so it is checked by the same Validate.
    private static ApplyLeaveRequest? ReadPreviewQuery(IQueryCollection query)
    {
        if (!int.TryParse(query["leaveTypeId"], NumberStyles.None, CultureInfo.InvariantCulture, out var leaveTypeId)
            || !TryDate(query["startDate"].ToString(), out var start)
            || !TryDate(query["endDate"].ToString(), out var end))
        {
            return null;
        }

        var halfDay = HalfDay.None;
        var halfDayText = query["halfDay"].ToString();
        if (halfDayText.Length > 0 && !Enum.TryParse(halfDayText, ignoreCase: false, out halfDay))
        {
            return null;
        }

        return new ApplyLeaveRequest(leaveTypeId, start, end, halfDay, Reason: null);

        static bool TryDate(string text, out DateOnly date) =>
            DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static async Task<ApplyLeaveRequest?> ReadBodyAsync(HttpRequest http, CancellationToken ct)
    {
        if (!http.HasJsonContentType())
        {
            return null;
        }

        // A leave request is tiny (the reason is at most 500 characters), so do not buffer more than this.
        // Without it the framework would read up to its 30 MB default before the reason limit is checked.
        var limit = http.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
        {
            limit.MaxRequestBodySize = MaxBodyBytes;
        }

        try
        {
            return await http.ReadFromJsonAsync<ApplyLeaveRequest>(ct);
        }
        catch (Exception ex) when (ex is JsonException or BadHttpRequestException)
        {
            // Wrong shape, an unknown halfDay text, a bad date, no body at all, or a body over the limit.
            return null;
        }
    }

    // Shape checks only. Every business rule lives in the use case and the Domain.
    private static string? Validate(ApplyLeaveRequest body)
    {
        if (body.LeaveTypeId <= 0)
        {
            return "leaveTypeId is required.";
        }

        if (!Enum.IsDefined(body.HalfDay))
        {
            return "halfDay must be None, FirstHalf or SecondHalf.";
        }

        if (body.StartDate < EarliestDate || body.StartDate > LatestDate
            || body.EndDate < EarliestDate || body.EndDate > LatestDate)
        {
            return "startDate and endDate must be real dates between 2000 and 2100.";
        }

        if (body.Reason is { Length: > ReasonMaxLength })
        {
            return $"reason must be at most {ReasonMaxLength} characters.";
        }

        return null;
    }

    private static IResult Unauthorized() =>
        Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");

    private static IResult BadRequest(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad request", detail: detail);

    // 409 for an overlap (a conflict with another request), 422 for every other broken rule.
    private static IResult Refused(LeaveRuleError error)
    {
        var overlap = error.Code == LeaveRuleCodes.Overlap;
        return Results.Problem(
            statusCode: overlap ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity,
            title: overlap ? "Conflict" : "Request refused",
            detail: error.Message,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
