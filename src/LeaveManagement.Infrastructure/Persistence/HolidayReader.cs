using LeaveManagement.Application.Leave;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Reads balances.Holiday for one location. The unique index (LocationId, HolidayDate) serves the range seek.</summary>
internal sealed class HolidayReader(LeaveDbContext db) : IHolidayReader
{
    public async Task<IReadOnlySet<DateOnly>> GetHolidaysAsync(
        int locationId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var dates = await db.Holidays
            .AsNoTracking()
            .Where(h => h.LocationId == locationId && h.HolidayDate >= from && h.HolidayDate <= to)
            .Select(h => h.HolidayDate)
            .ToListAsync(ct);

        return dates.ToHashSet();
    }
}
