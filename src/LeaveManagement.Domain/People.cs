namespace LeaveManagement.Domain;

// Schema "people".

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
}

public class Employee
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>Entra ID object id. Null in dev auth mode.</summary>
    public string? ExternalId { get; set; }

    public int LocationId { get; set; }

    /// <summary>The person's manager. Null for the top of the tree.</summary>
    public int? ManagerId { get; set; }

    public DateOnly JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

public class EmployeeRole
{
    public int EmployeeId { get; set; }
    public Role Role { get; set; }
}
