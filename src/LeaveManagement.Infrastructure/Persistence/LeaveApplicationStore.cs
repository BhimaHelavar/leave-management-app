using System.Globalization;
using System.Text.Json;
using LeaveManagement.Application.Auditing;
using LeaveManagement.Application.Leave;
using LeaveManagement.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// Saves one leave application in one database transaction (data-model.md, "Sequence of the single transaction").
/// It touches leave.*, balances.* and audit.* on purpose: constitution Principle III (1.2.0) allows this one
/// named cross-module store, because the request, the deduction and the audit row must succeed or fail together.
/// It uses the same scoped <see cref="LeaveDbContext"/> as <see cref="AuditWriter"/>, so the audit row joins
/// the transaction.
/// </summary>
internal sealed class LeaveApplicationStore(
    LeaveDbContext db, TimeProvider time, IAuditWriter audit) : ILeaveApplicationStore
{
    public Task<ApplyResult> SaveAsync(LeaveApplication application, CancellationToken ct)
    {
        // A user-started transaction must run inside the execution strategy. None retries today
        // (UseSqlServer without retry), but this stays correct if one is turned on later.
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => RunAsync(application, ct));
    }

    private async Task<ApplyResult> RunAsync(LeaveApplication a, CancellationToken ct)
    {
        // A retried attempt starts with a clean change tracker.
        db.ChangeTracker.Clear();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1. A repeated key returns the earlier answer; the same key with another body is refused.
        var replay = await FindSavedAnswerAsync(a.EmployeeId, a.IdempotencyKey, a.RequestHash, ct);
        if (replay is not null)
        {
            return replay;
        }

        var now = time.GetUtcNow().UtcDateTime;
        var request = a.Request;
        var leaveYear = (short)request.StartDate.Year;

        // The usual overlap gets a clear answer here, before anything is written. The unique index
        // in step 3 stays as the backstop for two submits that race each other.
        var wanted = a.Slots.Select(s => (s.Date, s.Slot)).ToHashSet();
        var taken = await db.LeaveRequestDays
            .AsNoTracking()
            .Where(d => d.EmployeeId == a.EmployeeId
                && d.LeaveDate >= request.StartDate && d.LeaveDate <= request.EndDate)
            .Select(d => new { d.LeaveDate, d.Slot })
            .ToListAsync(ct);
        if (taken.Any(d => wanted.Contains((d.LeaveDate, d.Slot))))
        {
            // The days may be taken by the very request this key already saved: a parallel twin that
            // committed after step 1 looked. Then this is a retry, and the answer is the saved one.
            var twin = await FindSavedAnswerAsync(a.EmployeeId, a.IdempotencyKey, a.RequestHash, ct);
            return twin ?? ApplyResult.Fail(LeaveRuleErrors.Overlap());
        }

        // 2. The request. Status is always Pending; the client cannot set it.
        var entity = new LeaveRequest
        {
            EmployeeId = a.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            HalfDay = request.HalfDay,
            DaysCount = a.DaysCount,
            Status = LeaveStatus.Pending,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            SubmittedAtUtc = now,
        };
        db.LeaveRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        // 3. One row per half-day slot. The unique index (EmployeeId, LeaveDate, Slot) settles an
        // overlap, including two submits that race each other (rule 13).
        foreach (var slot in a.Slots)
        {
            db.LeaveRequestDays.Add(new LeaveRequestDay
            {
                RequestId = entity.Id,
                EmployeeId = a.EmployeeId,
                LeaveDate = slot.Date,
                Slot = slot.Slot,
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await RollbackAsync(tx, ct);

            // Two requests with the same key and body race: both pass step 1, both insert their own request,
            // and the loser hits the day index here, only after the winner committed its answer. So look for
            // that answer first. It is an overlap only when no answer for this key exists.
            var winner = await FindSavedAnswerAsync(a.EmployeeId, a.IdempotencyKey, a.RequestHash, ct);
            return winner ?? ApplyResult.Fail(LeaveRuleErrors.Overlap());
        }

        if (a.HasBalance)
        {
            // 4. One guarded atomic update. Zero rows means not enough days, or no balance row at all.
            // Never read the balance, subtract in code and write it back.
            var days = a.DaysCount;
            var changed = await db.LeaveBalances
                .Where(b => b.EmployeeId == a.EmployeeId
                    && b.LeaveTypeId == request.LeaveTypeId
                    && b.LeaveYear == leaveYear
                    && b.Available >= days)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Available, b => b.Available - days), ct);

            if (changed == 0)
            {
                await RollbackAsync(tx, ct);
                return ApplyResult.Fail(LeaveRuleErrors.BalanceInsufficient());
            }

            // 5. The ledger entry the dashboard derives "pending" from (US-02).
            db.LeaveBalanceEntries.Add(new LeaveBalanceEntry
            {
                EmployeeId = a.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                LeaveYear = leaveYear,
                EntryType = LedgerEntryType.Deduction,
                Days = -days,
                RequestId = entity.Id,
                CreatedAtUtc = now,
                CreatedByEmployeeId = a.EmployeeId,
            });
        }

        // 6. The audit row holds ids, dates and the day count. Never the reason, a name or an email.
        // It saves the ledger entry above in the same call.
        await audit.WriteAsync(
            new AuditRecord(
                Action: "leave.apply",
                EntityType: "LeaveRequest",
                EntityId: entity.Id.ToString(CultureInfo.InvariantCulture),
                ActorEmployeeId: a.EmployeeId,
                Detail: new
                {
                    leaveTypeId = request.LeaveTypeId,
                    startDate = request.StartDate,
                    endDate = request.EndDate,
                    halfDay = request.HalfDay.ToString(),
                    days = a.DaysCount,
                }),
            ct);

        // 7. Remember the answer for a retry with the same key.
        var response = new LeaveRequestResponse(
            entity.Id,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.HalfDay,
            a.DaysCount,
            LeaveStatus.Pending.ToString(),
            leaveYear);

        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            EmployeeId = a.EmployeeId,
            KeyValue = a.IdempotencyKey,
            RequestHash = a.RequestHash,
            StatusCode = 201,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAtUtc = now,
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A parallel request with the same key committed first. Undo ours and return its answer,
            // so a double click sees the original result and not an error.
            await RollbackAsync(tx, ct);
            var winner = await FindSavedAnswerAsync(a.EmployeeId, a.IdempotencyKey, a.RequestHash, ct);
            if (winner is not null)
            {
                return winner;
            }

            throw;
        }

        await tx.CommitAsync(ct);
        return ApplyResult.Ok(response);
    }

    public async Task<ApplyResult?> FindSavedAnswerAsync(
        int employeeId, string idempotencyKey, string requestHash, CancellationToken ct)
    {
        // Always filtered by the person, so one person can never replay or collide with another's key.
        var saved = await db.IdempotencyKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.EmployeeId == employeeId && k.KeyValue == idempotencyKey, ct);

        if (saved is null)
        {
            return null;
        }

        if (saved.RequestHash != requestHash)
        {
            return ApplyResult.Fail(LeaveRuleErrors.IdempotencyMismatch());
        }

        var response = JsonSerializer.Deserialize<LeaveRequestResponse>(saved.ResponseJson);
        return response is null ? null : ApplyResult.Ok(response);
    }

    private async Task RollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx, CancellationToken ct)
    {
        await tx.RollbackAsync(ct);
        db.ChangeTracker.Clear();
    }

    // 2601 = duplicate in a unique index, 2627 = duplicate in a primary or unique key.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
