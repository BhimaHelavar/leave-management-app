# Leave request lifecycle

States come from `LeaveStatus` (`Pending`, `Approved`, `Rejected`, `Cancelled`). Rules come from the 15 rules in `ai/steering/product.md`. Apply, approve and cancel endpoints arrive in 4d and 4e, so this is intended behaviour, not yet code.

```mermaid
stateDiagram-v2
    [*] --> Pending: employee applies, days deducted at submit
    Pending --> Approved: manager approves (direct reports, never own)
    Pending --> Rejected: approver rejects, comment required, days restored
    Pending --> Cancelled: employee cancels before start, days restored
    Approved --> Cancelled: employee before start, HR after start, days restored
    Rejected --> [*]
    Cancelled --> [*]
```

HR approves on behalf when the approver is absent or the person has no manager (rule 14); HR approvals are logged.

## Checks at apply time (server side)

```mermaid
flowchart TD
    A[Apply] --> B{Dates valid, inside one leave year?}
    B -- no --> X[422 rule failure]
    B -- yes --> C{Overlaps a pending or approved request?}
    C -- yes --> Y[409 conflict]
    C -- no --> D{Type rules: probation, notice, max consecutive, medical cert, half day}
    D -- fail --> X
    D -- pass --> E{Balance stays at or above zero?}
    E -- no --> X
    E -- yes --> F[Insert request, days, ledger entry and audit row in one transaction]
```

## Facts to remember

- `DaysCount` is a snapshot and is never recalculated.
- The overlap rule is also a unique index on `leave.LeaveRequestDay` (second line of defence).
- Every apply, approve, reject, cancel and balance change writes an audit row in the same transaction.
