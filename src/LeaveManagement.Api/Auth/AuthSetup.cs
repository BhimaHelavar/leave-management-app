using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Auth;

public static class AuthSetup
{
    /// <summary>The claim names are the same in Dev and Entra ID mode, so business code never changes.</summary>
    public const string RoleClaim = "role";
    public const string NameClaim = "name";

    /// <summary>
    /// The one place that reads Auth:Mode. Fails at start-up (not at the first request) when the
    /// settings are unsafe: Dev mode is allowed only in Development or Demo (security rule 6),
    /// and the signing key must be long enough.
    /// </summary>
    public static IServiceCollection AddLeaveAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var mode = configuration["Auth:Mode"] ?? "Dev";

        if (mode == "EntraId")
        {
            throw new InvalidOperationException(
                "Auth:Mode 'EntraId' arrives in increment 4g. Use 'Dev' until then.");
        }

        if (mode != "Dev")
        {
            throw new InvalidOperationException($"Auth:Mode must be 'Dev' or 'EntraId', but was '{mode}'.");
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Demo"))
        {
            throw new InvalidOperationException(
                $"Auth:Mode 'Dev' is allowed only in the Development or Demo environment, but this is '{environment.EnvironmentName}'.");
        }

        var options = configuration.GetSection(DevTokenOptions.SectionName).Get<DevTokenOptions>() ?? new();
        if (options.SigningKey.Length < DevTokenOptions.MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Auth:Dev:SigningKey must be at least {DevTokenOptions.MinimumKeyLength} characters. " +
                "Set it in user-secrets or an environment variable; never commit a real key.");
        }

        if (options.LifetimeMinutes is < 1 or > 480)
        {
            throw new InvalidOperationException("Auth:Dev:LifetimeMinutes must be between 1 and 480.");
        }

        services.Configure<DevTokenOptions>(configuration.GetSection(DevTokenOptions.SectionName));
        services.AddScoped<DevTokenService>();

        // Writes an AccessDenied audit row whenever a signed-in person is refused for a missing role.
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditingAuthorizationResultHandler>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                // Keep the claim names exactly as issued (sub, name, email, role).
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = NameClaim,
                    RoleClaimType = RoleClaim,
                };
            });

        services.AddAuthorizationBuilder()
            // Deny by default: every endpoint needs a signed-in person unless it says AllowAnonymous.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.RequireManager, p => p.RequireRole("Manager"))
            .AddPolicy(Policies.RequireHR, p => p.RequireRole("HR"))
            .AddPolicy(Policies.RequireAdmin, p => p.RequireRole("Admin"));

        // The dev sign-in endpoints are anonymous, so they are rate limited per client address.
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(Policies.DevSignIn, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
