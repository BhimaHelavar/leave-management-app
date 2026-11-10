namespace LeaveManagement.Application.Auditing;

/// <summary>
/// One fact to append to the audit log. Holds ids and values only: never a name, an email,
/// a token or free text (security rule 12).
/// </summary>
/// <param name="Detail">Small object of ids and values; stored as JSON. Null means "{}".</param>
public sealed record AuditRecord(
    string Action,
    string EntityType,
    string EntityId,
    int? ActorEmployeeId,
    object? Detail = null,
    string? CorrelationId = null);

/// <summary>Appends a row to the audit log. The log is insert-only: there is no update or delete.</summary>
public interface IAuditWriter
{
    Task WriteAsync(AuditRecord record, CancellationToken ct = default);
}
