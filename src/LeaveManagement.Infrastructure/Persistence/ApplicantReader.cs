using LeaveManagement.Application.Leave;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>Reads the people module (Employee, Location) for one person.</summary>
internal sealed class ApplicantReader(LeaveDbContext db) : IApplicantReader
{
    public Task<ApplicantInfo?> GetAsync(int employeeId, CancellationToken ct) =>
        (from employee in db.Employees.AsNoTracking()
         join location in db.Locations.AsNoTracking() on employee.LocationId equals location.Id
         where employee.Id == employeeId
         select new ApplicantInfo(employee.Id, employee.LocationId, location.TimeZoneId, employee.IsActive))
        .FirstOrDefaultAsync(ct);
}
