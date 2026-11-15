using System.Globalization;
using System.Text.Json;
using LeaveManagement.Application.Approvals;
using LeaveManagement.Application.Auditing;
using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Approves or rejects one request in one database transaction (data-model.md, "What a decision touches").
/// It touches leave.*, balances.*, audit.* and notify.* on purpose: constitution Principle III (1.2.0) allows this
/// named cross-module store, because the decision, the restored days, the audit row and the outbox row must succeed
/// or fail together. It uses the same scoped <see cref="LeaveDbContext"/> as <see cref="AuditWriter"/>, so the audit
/// row joins the transaction.
/// </summary>
internal sealed class LeaveDecisionStore(
    LeaveDbContext db, TimeProvider time, IAuditWriter audit) : ILeaveDecisionStore
{
    private const string OutboxType = "leave.decided";

    public Task<DecisionResult> DecideAsync(LeaveDecisionCommand command, CancellationToken ct)
    {
        // A user-started transaction must run inside the execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => RunAsync(command, ct));
    }

    private async Task<DecisionResult> RunAsync(LeaveDecisionCommand c, CancellationToken ct)
    {
        // A retried attempt starts with a clean change tracker.
        db.ChangeTracker.Clear();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var now = time.GetUtcNow().UtcDateTime;
        var isReject = c.Outcome == DecisionOutcome.Reject;
        var newStatus = isReject ? LeaveStatus.Rejected : LeaveStatus.Approved;
        var decidedBy = (int?)c.DecidedByEmployeeId;
        var decidedAt = (DateTime?)now;
        var comment = c.Comment;

        // 1. One guarded update. Only a request that is still pending can be decided, so a lost race
        // (another decision or a cancel committed first) changes zero rows.
        var changed = await db.LeaveRequests
            .Where(r => r.Id == c.RequestId && r.Status == LeaveStatus.Pending)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(r => r.Status, newStatus)
                    .SetProperty(r => r.DecidedByEmployeeId, decidedBy)
                    .SetProperty(r => r.DecidedAtUtc, decidedAt)
                    .SetProperty(r => r.DecisionComment, comment),
                ct);

        if (changed == 0)
        {
            await RollbackAsync(tx, ct);
            return DecisionResult.NotPending();
        }

        if (isReject)
        {
            // 2. The dates become free again. The unique index stays.
            await db.LeaveRequestDays
                .Where(d => d.RequestId == c.RequestId)
                .ExecuteDeleteAsync(ct);

            if (c.HasBalance)
            {
                // 3. One atomic update, never read-add-write. The saved DaysCount is used, never recalculated.
                var days = c.Days;
                var leaveYear = (short)c.LeaveYear;
                var restored = await db.LeaveBalances
                    .Where(b => b.EmployeeId == c.OwnerEmployeeId
                        && b.LeaveTypeId == c.LeaveTypeId
                        && b.LeaveYear == leaveYear)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.Available, b => b.Available + days), ct);

                if (restored == 0)
                {
                    // The days were taken at submit, so a missing balance row is a fault, not a refusal.
                    // Never lose the days silently.
                    await RollbackAsync(tx, ct);
                    throw new InvalidOperationException(
                        $"No balance row to restore for request {c.RequestId} (type {c.LeaveTypeId}, year {c.LeaveYear}).");
                }

                db.LeaveBalanceEntries.Add(new LeaveBalanceEntry
                {
                    EmployeeId = c.OwnerEmployeeId,
                    LeaveTypeId = c.LeaveTypeId,
                    LeaveYear = leaveYear,
                    EntryType = LedgerEntryType.Restore,
                    Days = days,
                    RequestId = c.RequestId,
                    CreatedAtUtc = now,
                    CreatedByEmployeeId = c.DecidedByEmployeeId,
                });
            }
        }

        // 4. The audit row holds ids and numbers only. Never the comment, a name or an email.
        // It saves the ledger entry above in the same call.
        await audit.WriteAsync(
            new AuditRecord(
                Action: isReject ? "leave.reject" : "leave.approve",
                EntityType: "LeaveRequest",
                EntityId: c.RequestId.ToString(CultureInfo.InvariantCulture),
                ActorEmployeeId: c.DecidedByEmployeeId,
                Detail: new
                {
                    requestId = c.RequestId,
                    leaveTypeId = c.LeaveTypeId,
                    outcome = newStatus.ToString(),
                    days = c.Days,
                }),
            ct);

        // 5. The outbox row, ids only. Delivery is increment 4g.
        db.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Type = OutboxType,
            Payload = JsonSerializer.Serialize(new
            {
                requestId = c.RequestId,
                employeeId = c.OwnerEmployeeId,
                outcome = newStatus.ToString(),
            }),
            CreatedAtUtc = now,
            Attempts = 0,
        });

        // 6. Save and commit.
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return DecisionResult.Success();
    }

    private async Task RollbackAsync(IDbContextTransaction tx, CancellationToken ct)
    {
        await tx.RollbackAsync(ct);
        db.ChangeTracker.Clear();
    }
}
