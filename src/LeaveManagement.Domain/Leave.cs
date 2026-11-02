namespace LeaveManagement.Domain;

// Schema "leave".

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public HalfDay HalfDay { get; set; } = HalfDay.None;

    /// <summary>Snapshot taken at submit, so later rule or holiday changes never rewrite history.</summary>
    public decimal DaysCount { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    /// <summary>Computed by the database from StartDate. Read only.</summary>
    public short LeaveYear { get; private set; }

    public string? Reason { get; set; }
    public DateTime SubmittedAtUtc { get; set; }

    /// <summary>For a cancelled request, the person who cancelled it.</summary>
    public int? DecidedByEmployeeId { get; set; }

    public DateTime? DecidedAtUtc { get; set; }
    public string? DecisionComment { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>One row per working half-day a request covers. Its unique index makes overlap impossible.</summary>
public class LeaveRequestDay
{
    public int RequestId { get; set; }

    /// <summary>A deliberate copy of the request's EmployeeId, needed by the overlap index.</summary>
    public int EmployeeId { get; set; }

    public DateOnly LeaveDate { get; set; }
    public Slot Slot { get; set; }
}

/// <summary>Remembers the answer to a repeated request that carried the same Idempotency-Key.</summary>
public class IdempotencyKey
{
    public int EmployeeId { get; set; }
    public string KeyValue { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
