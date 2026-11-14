using LeaveManagement.Domain;

namespace LeaveManagement.Application.Leave;

/// <summary>Everything the store needs to save one application. The count and slots were decided by the server.</summary>
public sealed record LeaveApplication(
    int EmployeeId,
    string IdempotencyKey,
    string RequestHash,
    ApplyLeaveRequest Request,
    IReadOnlyList<LeaveSlot> Slots,
    decimal DaysCount,
    bool HasBalance);

/// <summary>
/// Saves one leave application in a single database transaction: the request, its day rows, the balance
/// deduction, the ledger entry, the audit row and the idempotency answer succeed together or not at all.
/// This is the one named cross-module store that constitution Principle III (1.2.0) allows.
/// </summary>
public interface ILeaveApplicationStore
{
    /// <summary>
    /// Returns the saved request (new, or the earlier answer for a repeated key), or a refusal:
    /// <c>leave.overlap</c>, <c>balance.insufficient</c> or <c>idempotency.mismatch</c>.
    /// </summary>
    Task<ApplyResult> SaveAsync(LeaveApplication application, CancellationToken ct);

    /// <summary>
    /// The earlier answer for this person's key, or null when the key is new. The same key with another body
    /// gives <c>idempotency.mismatch</c>. A use case asks this before it plans, so a retry of a request that
    /// already succeeded gets the saved answer even if today's rules (the date, the notice) would now refuse it.
    /// </summary>
    Task<ApplyResult?> FindSavedAnswerAsync(
        int employeeId, string idempotencyKey, string requestHash, CancellationToken ct);
}
