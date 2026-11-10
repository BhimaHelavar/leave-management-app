using LeaveManagement.Application.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LeaveManagement.Api.Auth;

/// <summary>
/// Runs after the authorization decision. When a signed-in person is refused (403) because they lack
/// a role, it writes one <c>AccessDenied</c> audit row, then lets the framework answer 403.
/// One place covers every protected endpoint, so no endpoint has to remember to audit.
/// A request with no valid token (401) is not audited: there is no known actor and it would only add noise.
/// </summary>
public sealed class AuditingAuthorizationResultHandler(ILogger<AuditingAuthorizationResultHandler> logger)
    : IAuthorizationMiddlewareResultHandler
{
    public const string AccessDeniedAction = "AccessDenied";

    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            await TryAuditAsync(context);
        }

        // Challenge (401), Forbid (403) and success all follow the framework's normal behaviour.
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private async Task TryAuditAsync(HttpContext context)
    {
        try
        {
            var endpoint = context.GetEndpoint();
            // The route pattern, not the raw URL: it never carries ids or query text typed by the client.
            var route = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? "unknown";
            var policyName = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Select(a => a.Policy)
                .FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? "fallback";

            var subject = context.User.FindFirst("sub")?.Value;
            int? actor = int.TryParse(subject, out var id) ? id : null;

            var writer = context.RequestServices.GetRequiredService<IAuditWriter>();
            await writer.WriteAsync(
                new AuditRecord(
                    AccessDeniedAction,
                    "Endpoint",
                    route,
                    actor,
                    new { policy = policyName },
                    context.TraceIdentifier),
                context.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A broken audit log must not turn a refusal into a server error. Ids only, no claims or token.
            logger.LogError(ex, "Could not write the AccessDenied audit row. TraceId {TraceId}", context.TraceIdentifier);
        }
    }
}
