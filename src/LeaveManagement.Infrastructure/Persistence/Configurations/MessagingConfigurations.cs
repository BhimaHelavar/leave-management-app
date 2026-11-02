using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

// Schemas "notify" and "audit".

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessage", "notify");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(60).IsUnicode(false).IsRequired();
        b.Property(x => x.Payload).HasColumnType("nvarchar(max)");
        b.Property(x => x.LastError).HasMaxLength(500);

        b.HasIndex(x => x.MessageId).IsUnique();

        // Outbox worker: the oldest unsent messages. Stays small however many were sent.
        b.HasIndex(x => x.Id)
            .HasDatabaseName("IX_OutboxMessage_Unsent")
            .HasFilter("[SentAtUtc] IS NULL");
    }
}

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> b)
    {
        b.ToTable("ProcessedMessage", "notify");

        // A consumer checks this first, so a message delivered twice sends one email.
        b.HasKey(x => new { x.Consumer, x.MessageId });
        b.Property(x => x.Consumer).HasMaxLength(50).IsUnicode(false);
    }
}

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("AuditEvent", "audit");

        // Clustered primary key on an ever-growing Id: new rows always go at the end.
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(60).IsUnicode(false).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.DetailJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.CorrelationId).HasMaxLength(64).IsUnicode(false);

        // Audit log screen (S-10): newest events, filtered by person or action. Keyset paging on Id.
        b.HasIndex(x => new { x.ActorEmployeeId, x.Id }).IsDescending(false, true);
        b.HasIndex(x => new { x.Action, x.Id }).IsDescending(false, true);

        // No foreign keys: audit points at business rows only by id, so it survives anything.
    }
}
