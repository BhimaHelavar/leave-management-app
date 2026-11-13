using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LeaveManagement.Application.Leave;

/// <summary>
/// Use case: the signed-in person applies for leave. The server counts the working days and checks the
/// rules (through <see cref="LeavePlanner"/>, shared with the preview); nothing the client sends decides the
/// count, the status or the owner (FR-005, FR-015).
/// </summary>
public sealed class ApplyForLeave(
    IApplicantReader applicants, LeavePlanner planner, ILeaveApplicationStore store)
{
    /// <returns>The saved request or the refusal. Null when the person is unknown or inactive (the endpoint answers 401).</returns>
    public async Task<ApplyResult?> ExecuteAsync(
        int employeeId, string idempotencyKey, ApplyLeaveRequest request, CancellationToken ct)
    {
        // A deactivated person gets nothing back, not even their own earlier answers.
        if (await applicants.GetAsync(employeeId, ct) is not { IsActive: true })
        {
            return null;
        }

        // A retry of a request that already succeeded gets the saved answer before any rule runs. Otherwise a
        // retry after midnight, or after the leave type was switched off, would be refused for a request
        // that is in fact saved, and the person would send it again under a new key.
        var requestHash = Hash(request);
        var replay = await store.FindSavedAnswerAsync(employeeId, idempotencyKey, requestHash, ct);
        if (replay is not null)
        {
            return replay;
        }

        var outcome = await planner.PlanAsync(
            employeeId, request.LeaveTypeId, request.StartDate, request.EndDate, request.HalfDay, ct);

        if (outcome.UnknownPerson)
        {
            return null;
        }

        if (outcome.Error is { } error)
        {
            return ApplyResult.Fail(error);
        }

        var plan = outcome.Plan!;
        return await store.SaveAsync(
            new LeaveApplication(
                employeeId,
                idempotencyKey,
                requestHash,
                request,
                plan.Counted.Slots,
                plan.Counted.DaysCount,
                plan.Type.HasBalance),
            ct);
    }

    /// <summary>Fingerprint of the body, so the same key with a different body is told apart. 64 hex characters.</summary>
    private static string Hash(ApplyLeaveRequest r)
    {
        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"{r.LeaveTypeId}|{r.StartDate:yyyy-MM-dd}|{r.EndDate:yyyy-MM-dd}|{r.HalfDay}|{r.Reason?.Trim()}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
