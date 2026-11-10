using LeaveManagement.Api.Auth;

namespace LeaveManagement.Api.Admin;

public static class AdminEndpoints
{
    /// <summary>
    /// A probe that only an Admin may call. It lets us show that a missing role gives 403 and an audit row
    /// before the real Admin features (leave types, holidays) exist. Remove it when those arrive.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/ping", () => Results.NoContent())
            .RequireAuthorization(Policies.RequireAdmin)
            .WithName("AdminPing");

        return app;
    }
}
