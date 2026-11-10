using System.Security.Claims;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Auth;

public sealed record MeResponse(int Id, string Name, string Email, string[] Roles);

public sealed record DevUserResponse(int Id, string FullName, string Email, string[] Roles);

public sealed record TokenRequest(int EmployeeId);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, MeResponse User);

public static class AuthEndpoints
{
    /// <summary>/api/me works for any signed-in person. The dev endpoints exist only in Dev mode.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, bool mapDevEndpoints)
    {
        // No policy here on purpose: the fallback policy already requires a signed-in person.
        app.MapGet("/api/me", (ClaimsPrincipal user) =>
        {
            var id = user.FindFirstValue("sub");
            if (!int.TryParse(id, out var employeeId))
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
            }

            return Results.Ok(new MeResponse(
                employeeId,
                user.FindFirstValue(AuthSetup.NameClaim) ?? string.Empty,
                user.FindFirstValue("email") ?? string.Empty,
                user.FindAll(AuthSetup.RoleClaim).Select(c => c.Value).Order().ToArray()));
        }).WithName("Me");

        if (!mapDevEndpoints)
        {
            return app;
        }

        app.MapGet("/api/dev/users", async (LeaveDbContext db, DevTokenService tokens, CancellationToken ct) =>
            {
                var people = await db.Employees
                    .AsNoTracking()
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.FullName)
                    .Select(e => new { e.Id, e.FullName, e.Email })
                    .ToListAsync(ct);

                var result = new List<DevUserResponse>(people.Count);
                foreach (var person in people)
                {
                    result.Add(new DevUserResponse(
                        person.Id, person.FullName, person.Email, await tokens.RolesOfAsync(person.Id, ct)));
                }

                return Results.Ok(result);
            })
            .AllowAnonymous()
            .RequireRateLimiting(Policies.DevSignIn)
            .WithName("DevUsers");

        app.MapPost("/api/dev/token", async (TokenRequest request, DevTokenService tokens, CancellationToken ct) =>
            {
                var response = await tokens.IssueAsync(request.EmployeeId, ct);
                return response is null
                    ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Person not found")
                    : Results.Ok(response);
            })
            .AllowAnonymous()
            .RequireRateLimiting(Policies.DevSignIn)
            .WithName("DevToken");

        return app;
    }
}
