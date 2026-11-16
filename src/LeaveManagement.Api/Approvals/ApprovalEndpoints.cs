using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using LeaveManagement.Api.Auth;
using LeaveManagement.Application.Approvals;
using LeaveManagement.Domain;

namespace LeaveManagement.Api.Approvals;

public static class ApprovalEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MaxPage = 100000; // keeps (page - 1) * pageSize far inside int range
    private const long MaxBodyBytes = 4096;

    /// <summary>
    /// The manager inbox. Two layers (security rule 3): the Manager role policy decides who may call it, and the
    /// use case then lists only the caller's direct reports. The manager is the token's sub; the team is read from
    /// the database, and the client can send no person id, so there is nothing to probe or over-post.
    /// </summary>
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/approvals", async (
                ClaimsPrincipal user,
                HttpRequest http,
                GetManagerInbox useCase,
                CancellationToken ct) =>
            {
                if (!int.TryParse(user.FindFirstValue("sub"), out var managerId))
                {
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
                }

                // Read as text so a bad value is always a clean 400, in every environment.
                if (!TryReadInt(http.Query["page"].ToString(), 1, out var page) || page < 1 || page > MaxPage)
                {
                    return BadRequest($"page must be a whole number from 1 to {MaxPage}.");
                }

                if (!TryReadInt(http.Query["pageSize"].ToString(), DefaultPageSize, out var pageSize)
                    || pageSize < 1 || pageSize > MaxPageSize)
                {
                    return BadRequest($"pageSize must be a whole number from 1 to {MaxPageSize}.");
                }

                return Results.Ok(await useCase.ExecuteAsync(managerId, page, pageSize, ct));
            })
            .RequireAuthorization(Policies.RequireManager)
            .WithName("ManagerInbox");

        // The detail answers 404 for a missing request and for one the caller may not see, so ids cannot be probed.
        app.MapGet("/api/approvals/{requestId:int}", async (
                int requestId,
                ClaimsPrincipal user,
                GetApprovalDetail useCase,
                CancellationToken ct) =>
            {
                if (!int.TryParse(user.FindFirstValue("sub"), out var managerId))
                {
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
                }

                var detail = await useCase.ExecuteAsync(managerId, requestId, ct);
                return detail is null
                    ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found")
                    : Results.Ok(detail);
            })
            .RequireAuthorization(Policies.RequireManager)
            .WithName("GetApprovalDetail");

        // The manager is the token's sub. The body carries only the comment: no person id, status or time is read.
        app.MapPost("/api/approvals/{requestId:int}/reject", (
                int requestId,
                ClaimsPrincipal user,
                HttpRequest http,
                DecideLeaveRequest useCase,
                CancellationToken ct) =>
                DecideAsync(requestId, DecisionOutcome.Reject, user, http, useCase, ct))
            .RequireAuthorization(Policies.RequireManager)
            .WithName("RejectLeaveRequest");

        // The body is optional for an approval: no body means no comment.
        app.MapPost("/api/approvals/{requestId:int}/approve", (
                int requestId,
                ClaimsPrincipal user,
                HttpRequest http,
                DecideLeaveRequest useCase,
                CancellationToken ct) =>
                DecideAsync(requestId, DecisionOutcome.Approve, user, http, useCase, ct))
            .RequireAuthorization(Policies.RequireManager)
            .WithName("ApproveLeaveRequest");

        return app;
    }

    private static async Task<IResult> DecideAsync(
        int requestId,
        DecisionOutcome outcome,
        ClaimsPrincipal user,
        HttpRequest http,
        DecideLeaveRequest useCase,
        CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirstValue("sub"), out var managerId))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
        }

        var body = await ReadCommentAsync(http, ct);
        if (!body.Valid)
        {
            return BadRequest(
                $"The body must be JSON with an optional text comment of at most {LeaveDecision.MaxCommentLength} characters.");
        }

        return ToResult(await useCase.ExecuteAsync(managerId, requestId, outcome, body.Comment, ct));
    }

    /// <summary>
    /// Reads <c>{ "comment": "..." }</c> by hand so every malformed body is a clean 400 in every environment (the
    /// framework would throw in Development). An empty body is valid and means no comment. The body is never logged.
    /// </summary>
    private static async Task<(bool Valid, string? Comment)> ReadCommentAsync(HttpRequest http, CancellationToken ct)
    {
        // No body: Content-Length 0, or no Content-Length, no Content-Type and no chunked encoding (what a client
        // that sends no body at all does). Both mean "no comment".
        var noBody = http.ContentLength == 0
            || (http.ContentLength is null
                && string.IsNullOrEmpty(http.ContentType)
                && !http.Headers.ContainsKey("Transfer-Encoding"));
        if (noBody)
        {
            return (true, null);
        }

        if (!http.HasJsonContentType())
        {
            return (false, null);
        }

        // Do not buffer more than this. Without it the framework would read up to its 30 MB default.
        var limit = http.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
        {
            limit.MaxRequestBodySize = MaxBodyBytes;
        }

        try
        {
            using var doc = await JsonDocument.ParseAsync(http.Body, cancellationToken: ct);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (false, null);
            }

            if (!doc.RootElement.TryGetProperty("comment", out var comment)
                || comment.ValueKind == JsonValueKind.Null)
            {
                return (true, null);
            }

            // A number, object or array is not text.
            return comment.ValueKind == JsonValueKind.String ? (true, comment.GetString()) : (false, null);
        }
        catch (Exception ex) when (ex is JsonException or BadHttpRequestException)
        {
            // Not JSON, no body at all, or a body over the limit.
            return (false, null);
        }
    }

    /// <summary>The status mapping shared by approve and reject. The use case holds every rule.</summary>
    private static IResult ToResult(DecideResult result) => result.Status switch
    {
        DecideStatus.Ok => Results.Ok(result.Detail),
        DecideStatus.CommentTooLong => BadRequest(
            $"comment must be at most {LeaveDecision.MaxCommentLength} characters."),
        DecideStatus.CommentRequired when result.Error is { } error => Coded(
            StatusCodes.Status422UnprocessableEntity, "Request refused", error.Code, error.Message),
        DecideStatus.NotPending when result.Error is { } error => Coded(
            StatusCodes.Status409Conflict, "Conflict", error.Code, error.Message),
        DecideStatus.NotFound => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found"),
        _ => throw new InvalidOperationException($"Unexpected decision result {result.Status}."),
    };

    private static IResult Coded(int status, string title, string code, string detail) =>
        Results.Problem(
            statusCode: status,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    /// <summary>An empty value means "use the default"; anything else must be a whole number.</summary>
    private static bool TryReadInt(string text, int fallback, out int value)
    {
        if (text.Length == 0)
        {
            value = fallback;
            return true;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static IResult BadRequest(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Bad request", detail: detail);
}
