namespace LeaveManagement.Domain;

// Every enum is stored in the database as readable text with a CHECK constraint (ADR 14).
// Changing a name here means changing the data: add a migration.

/// <summary>Extra roles. Everyone is an Employee implicitly.</summary>
public enum Role
{
    Manager,
    HR,
    Admin,
}

public enum AccrualMode
{
    None,
    Monthly,
    Yearly,
    Manual,
}

public enum LedgerEntryType
{
    Grant,
    Accrual,
    CarryForward,
    Expiry,
    Deduction,
    Restore,
    Adjustment,
}

public enum HalfDay
{
    None,
    FirstHalf,
    SecondHalf,
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>A half-day slot of a working day.</summary>
public enum Slot
{
    AM,
    PM,
}
