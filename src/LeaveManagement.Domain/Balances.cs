namespace LeaveManagement.Domain;

// Schema "balances": leave policy and balances.

public class LeaveType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPaid { get; set; }

    /// <summary>Null means HR grants by hand, or there is no limit.</summary>
    public decimal? DaysPerYear { get; set; }

    public AccrualMode AccrualMode { get; set; }
    public decimal CarryForwardMaxDays { get; set; }
    public int? CarryForwardExpiryMonth { get; set; }
    public int? CarryForwardExpiryDay { get; set; }
    public int MinNoticeWorkingDays { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public int ProbationBlockMonths { get; set; }
    public bool AllowHalfDay { get; set; }
    public int? MedicalCertAfterDays { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class Holiday
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>The current balance: a fast copy of the ledger and the guard against going below zero.</summary>
public class LeaveBalance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public short LeaveYear { get; set; }
    public decimal Available { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>The ledger: one row per balance change. Never edited.</summary>
public class LeaveBalanceEntry
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public short LeaveYear { get; set; }
    public LedgerEntryType EntryType { get; set; }

    /// <summary>Signed: positive adds days, negative removes days.</summary>
    public decimal Days { get; set; }

    public int? RequestId { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Null when a job wrote the entry.</summary>
    public int? CreatedByEmployeeId { get; set; }
}
