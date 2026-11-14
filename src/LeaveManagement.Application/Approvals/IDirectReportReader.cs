namespace LeaveManagement.Application.Approvals;

public sealed record ReportInfo(int EmployeeId, string FullName);

/// <summary>Port for the people module. Reads people.* tables only.</summary>
public interface IDirectReportReader
{
    /// <summary>
    /// The active people whose manager is this person, in name order. Never the manager. Read from the database
    /// on every call, so a changed reporting line applies at once (security rule 7).
    /// </summary>
    Task<IReadOnlyList<ReportInfo>> GetAsync(int managerId, CancellationToken ct);
}
