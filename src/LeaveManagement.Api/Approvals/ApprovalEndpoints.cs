using System.Globalization;
using System.Security.Claims;
using LeaveManagement.Api.Auth;
using LeaveManagement.Application.Approvals;

namespace LeaveManagement.Api.Approvals;

public static class ApprovalEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

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
                if (!TryReadInt(http.Query["page"].ToString(), 1, out var page) || page < 1)
                {
                    return BadRequest("page must be a whole number, 1 or more.");
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

        return app;
    }

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
