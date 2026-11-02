using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Helpers that store an enum as readable text and build the matching CHECK list.</summary>
public static class EnumColumn
{
    /// <summary>Stores the enum as varchar text (for example "Approved", not 2).</summary>
    public static PropertyBuilder<TEnum> AsText<TEnum>(this PropertyBuilder<TEnum> property, int maxLength)
        where TEnum : struct, Enum
        => property.HasConversion<string>().HasMaxLength(maxLength).IsUnicode(false);

    /// <summary>Builds: [Column] IN ('A', 'B', ...) from the enum's names, so the CHECK can never drift from the C# enum.</summary>
    public static string InList<TEnum>(string column)
        where TEnum : struct, Enum
        => $"[{column}] IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}
