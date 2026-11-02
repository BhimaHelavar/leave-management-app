using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LeaveManagement.Infrastructure.Persistence;

// A datetime2 column has no time zone, so a DateTime read back would have Kind = Unspecified.
// These converters mark every timestamp we read as UTC. We always write UTC (3b decision 7).

public sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public sealed class NullableUtcDateTimeConverter()
    : ValueConverter<DateTime?, DateTime?>(
        v => v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
