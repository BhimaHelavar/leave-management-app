namespace LeaveManagement.Api.Auth;

/// <summary>Names of the authorization policies. Endpoints use these, never raw role strings.</summary>
public static class Policies
{
    public const string RequireManager = nameof(RequireManager);
    public const string RequireHR = nameof(RequireHR);
    public const string RequireAdmin = nameof(RequireAdmin);

    /// <summary>Rate limiter policy for the anonymous dev sign-in endpoints.</summary>
    public const string DevSignIn = nameof(DevSignIn);
}
