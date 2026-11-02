namespace LeaveManagement.Domain;

// Schemas "notify" and "audit". These hold facts about what happened or what was sent,
// never business data, and never names, email addresses or free-text reasons.

public class OutboxMessage
{
    public long Id { get; set; }
    public Guid MessageId { get; set; }
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON with ids only (for example requestId, employeeId).</summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
}

public class ProcessedMessage
{
    public string Consumer { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}

/// <summary>Insert only. Permissions and a trigger that enforce this arrive with the audit story (4e).</summary>
public class AuditEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Null when a job did it. Not a foreign key on purpose: audit points at business rows only by id.</summary>
    public int? ActorEmployeeId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string DetailJson { get; set; } = "{}";
    public string? CorrelationId { get; set; }
}
