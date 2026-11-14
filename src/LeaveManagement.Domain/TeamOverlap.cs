namespace LeaveManagement.Domain;

/// <summary>One half-day slot a person holds through a pending or approved request.</summary>
public readonly record struct HeldSlot(int RequestId, int EmployeeId, DateOnly Date, Slot Slot);

/// <summary>Another person who is away on some of the same days, with the dates they share.</summary>
public sealed record PersonOverlap(int EmployeeId, IReadOnlyList<DateOnly> Dates);

/// <param name="Others">The other people with a shared slot, in id order. Empty when no one else is away.</param>
/// <param name="AwayIfApproved">The requester plus the others: how many would be away if the request were approved.</param>
public sealed record OverlapResult(IReadOnlyList<PersonOverlap> Others, int AwayIfApproved);

/// <summary>
/// Who else in the team is away when a request is approved (US-06). Pure: no clock, no database.
/// Two people overlap only on the same date and the same slot, so a first half and a second half on one date do
/// not overlap, while a half day and a full day do. Day rows exist only for working days, so weekends and
/// holidays never overlap.
/// </summary>
public static class TeamOverlap
{
    /// <param name="held">The held slots of the whole team inside the request's dates, including the request's own.</param>
    public static OverlapResult For(int requestId, int employeeId, IEnumerable<HeldSlot> held)
    {
        var rows = held.ToList();

        var own = rows
            .Where(h => h.RequestId == requestId)
            .Select(h => (h.Date, h.Slot))
            .ToHashSet();

        if (own.Count == 0)
        {
            return new OverlapResult([], 1);
        }

        var others = rows
            .Where(h => h.EmployeeId != employeeId && own.Contains((h.Date, h.Slot)))
            .GroupBy(h => h.EmployeeId)
            .OrderBy(g => g.Key)
            .Select(g => new PersonOverlap(g.Key, g.Select(h => h.Date).Distinct().Order().ToList()))
            .ToList();

        return new OverlapResult(others, 1 + others.Count);
    }
}
