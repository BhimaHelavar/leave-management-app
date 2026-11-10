namespace LeaveManagement.Api.Auth;

/// <summary>
/// Settings for the local token service (section "Auth:Dev"). The signing key is a throwaway value
/// kept in appsettings.Development.json; any other environment must supply its own key from
/// configuration (never from the repository).
/// </summary>
public sealed class DevTokenOptions
{
    public const string SectionName = "Auth:Dev";
    public const int MinimumKeyLength = 32;

    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; } = 60;
    public string Issuer { get; set; } = "leave-management-dev";
    public string Audience { get; set; } = "leave-management-api";
}
