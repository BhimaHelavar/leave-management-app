using System.Security.Claims;
using System.Text;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Auth;

/// <summary>Signs a local token for a demo person. Dev mode only; the claims match Entra ID mode.</summary>
public sealed class DevTokenService(
    LeaveDbContext db, IOptions<DevTokenOptions> options, TimeProvider time)
{
    private readonly DevTokenOptions settings = options.Value;

    /// <summary>Returns null when the person is unknown or inactive, so no token is issued.</summary>
    public async Task<TokenResponse?> IssueAsync(int employeeId, CancellationToken ct)
    {
        var person = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new { e.Id, e.FullName, e.Email })
            .SingleOrDefaultAsync(ct);

        if (person is null)
        {
            return null;
        }

        var roles = await RolesOfAsync(person.Id, ct);

        // The manager is not in the token: it can change, so it is looked up when a rule needs it.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, person.Id.ToString()),
            new(AuthSetup.NameClaim, person.FullName),
            new(JwtRegisteredClaimNames.Email, person.Email),
        };
        claims.AddRange(roles.Select(r => new Claim(AuthSetup.RoleClaim, r)));

        var now = time.GetUtcNow();
        var expires = now.AddMinutes(settings.LifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenResponse(token, expires, new MeResponse(person.Id, person.FullName, person.Email, roles));
    }

    /// <summary>Everyone is an Employee; the table holds only the extra roles.</summary>
    public async Task<string[]> RolesOfAsync(int employeeId, CancellationToken ct)
    {
        var extra = await db.EmployeeRoles
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .Select(r => r.Role.ToString())
            .ToListAsync(ct);

        return ["Employee", .. extra.Order()];
    }
}
