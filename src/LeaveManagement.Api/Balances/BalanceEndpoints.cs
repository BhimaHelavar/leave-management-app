using System.Security.Claims;
using LeaveManagement.Application.Balances;

namespace LeaveManagement.Api.Balances;

public static class BalanceEndpoints
{
    /// <summary>
    /// The caller's own balances. The person comes from the token (sub), never from the URL or body,
    /// so there is no id to probe (security rule 4). No role policy: everyone is an Employee, and the
    /// fallback policy already requires a signed-in person.
    /// </summary>
    public static IEndpointRouteBuilder MapBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me/balances", async (
                ClaimsPrincipal user, GetMyBalances useCase, CancellationToken ct) =>
            {
                if (!int.TryParse(user.FindFirstValue("sub"), out var employeeId))
                {
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
                }

                return Results.Ok(await useCase.ExecuteAsync(employeeId, ct));
            })
            .WithName("MyBalances");

        return app;
    }
}
