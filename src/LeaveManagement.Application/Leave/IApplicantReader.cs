namespace LeaveManagement.Application.Leave;

/// <param name="TimeZoneId">Of the person's location. Decides what "today" is for them.</param>
public sealed record ApplicantInfo(int EmployeeId, int LocationId, string TimeZoneId, bool IsActive);

/// <summary>Reads the people module: where the person works and whether they are active.</summary>
public interface IApplicantReader
{
    /// <summary>Null when there is no such person.</summary>
    Task<ApplicantInfo?> GetAsync(int employeeId, CancellationToken ct);
}
