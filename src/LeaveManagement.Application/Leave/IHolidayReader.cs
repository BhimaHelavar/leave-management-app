namespace LeaveManagement.Application.Leave;

/// <summary>Reads the holidays of one location from the balances module.</summary>
public interface IHolidayReader
{
    /// <summary>Holiday dates for the location from <paramref name="from"/> to <paramref name="to"/>, both included.</summary>
    Task<IReadOnlySet<DateOnly>> GetHolidaysAsync(
        int locationId, DateOnly from, DateOnly to, CancellationToken ct);
}
