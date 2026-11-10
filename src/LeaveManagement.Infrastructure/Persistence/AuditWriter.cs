using System.Text.Json;
using LeaveManagement.Application.Auditing;
using LeaveManagement.Domain;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Inserts one <see cref="AuditEvent"/>. It never updates or deletes. Text is cut to the column
/// sizes (see AuditEventConfiguration) so an unusually long route cannot make the insert fail.
/// </summary>
internal sealed class AuditWriter(LeaveDbContext db, TimeProvider time) : IAuditWriter
{
    private const int ActionMax = 60;
    private const int EntityTypeMax = 40;
    private const int EntityIdMax = 40;
    private const int CorrelationIdMax = 64;

    public async Task WriteAsync(AuditRecord record, CancellationToken ct = default)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            OccurredAtUtc = time.GetUtcNow().UtcDateTime,
            ActorEmployeeId = record.ActorEmployeeId,
            Action = Cut(record.Action, ActionMax),
            EntityType = Cut(record.EntityType, EntityTypeMax),
            EntityId = Cut(record.EntityId, EntityIdMax),
            DetailJson = record.Detail is null ? "{}" : JsonSerializer.Serialize(record.Detail),
            CorrelationId = record.CorrelationId is null ? null : Cut(record.CorrelationId, CorrelationIdMax),
        });

        await db.SaveChangesAsync(ct);
    }

    private static string Cut(string value, int max) => value.Length <= max ? value : value[..max];
}
