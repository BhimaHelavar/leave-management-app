using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Two demo holidays for the US-03 / SCRUM-10 checks, Development only: H on a Monday and W on a Wednesday in a
/// different week, both in the future. They make the Jira example "a holiday on the Monday gives 2" and the
/// counting table testable. It only adds rows. A holiday is skipped when one with the same name already exists
/// (so the dates stay fixed after the first run) or when the date already holds a holiday. It runs on every start.
/// </summary>
internal static class DemoHolidays
{
    private const string MondayName = "Demo Monday holiday (test data)";
    private const string WednesdayName = "Demo Wednesday holiday (test data)";

    public static async Task SeedAsync(
        LeaveDbContext db, TimeProvider time, ILogger logger, CancellationToken ct = default)
    {
        var locationId = await db.Locations
            .Where(l => l.Name == "Bengaluru")
            .Select(l => (int?)l.Id)
            .SingleOrDefaultAsync(ct);
        if (locationId is null)
        {
            return; // No location yet, so there is nothing to attach holidays to.
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        var monday = await EnsureAsync(locationId.Value, MondayName, FirstOnOrAfter(today.AddDays(14), DayOfWeek.Monday));
        var wednesday = await EnsureAsync(locationId.Value, WednesdayName, FirstOnOrAfter(monday.AddDays(14), DayOfWeek.Wednesday));

        await db.SaveChangesAsync(ct);

        // The owner needs the dates to run quickstart.md, so say them on every start.
        logger.LogInformation(
            "US-03 demo holidays: H (Monday) is {Monday}, W (Wednesday) is {Wednesday}.", monday, wednesday);

        async Task<DateOnly> EnsureAsync(int location, string name, DateOnly wanted)
        {
            var byName = await db.Holidays
                .AsNoTracking()
                .Where(h => h.LocationId == location && h.Name == name)
                .Select(h => (DateOnly?)h.HolidayDate)
                .FirstOrDefaultAsync(ct);
            if (byName is not null)
            {
                return byName.Value;
            }

            // Keep the unique index (LocationId, HolidayDate) happy. A real holiday on that day also counts as one.
            var onDate = await db.Holidays.AnyAsync(h => h.LocationId == location && h.HolidayDate == wanted, ct);
            if (!onDate)
            {
                db.Holidays.Add(new Holiday { LocationId = location, HolidayDate = wanted, Name = name });
            }

            return wanted;
        }
    }

    private static DateOnly FirstOnOrAfter(DateOnly date, DayOfWeek day)
    {
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
