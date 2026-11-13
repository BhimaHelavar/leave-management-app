# Feature Specification: See my leave balance per leave type

**Feature Branch**: `SCRUM-9-US-02-See-my-leave-balance-per-leave-type`

**Created**: 2026-11-10

**Status**: Draft

**Input**: User description: "read jira https://bhimashankarcse92.atlassian.net/browse/SCRUM-9" (Jira SCRUM-9, US-02, requirement BR-02 Must, epic SCRUM-7)

> Story: **As an** employee, **I want to** see my balance per leave type **so that** I know what I can take.
> Acceptance: (1) Given 10 Annual accrued and 2 used, when I open the dashboard, then Annual shows 8 available and 0 pending. (2) Given a pending request of 3 days, then available shows 5.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See my balance for each leave type on my dashboard (Priority: P1)

A signed-in person opens their dashboard and sees one balance card per leave type that has a balance. Each card shows how much has been accrued, how much is used, how much is pending approval, and how much is available to take now.

**Why this priority**: This is the whole story. An employee cannot decide how much leave to ask for without it.

**Independent Test**: Sign in as a person with a known Annual balance, open the dashboard, and check that accrued, used, pending and available match the figures held for that person.

**Acceptance Scenarios**:

1. **Given** I have 10 Annual days accrued, 2 used and nothing pending, **When** I open the dashboard, **Then** Annual shows 8 available and 0 pending.
2. **Given** I have 10 Annual days accrued, 2 used and one pending request of 3 days, **When** I open the dashboard, **Then** Annual shows 5 available and 3 pending.
3. **Given** I have balances for several leave types, **When** I open the dashboard, **Then** I see a separate figure set for each type, and a change in one type does not alter another.
4. **Given** I have a pending request that is then rejected or cancelled, **When** I reopen the dashboard, **Then** the days return to available and pending drops by the same amount.

---

### User Story 2 - Understand leave types that behave differently (Priority: P2)

The dashboard treats leave types that have no balance, or a limited-life balance, in a way that is not misleading.

**Why this priority**: The story's two examples cover Annual only. The policy rules add types with different behaviour, and a wrong figure here would cause wrong decisions.

**Independent Test**: Sign in as a person with Annual carry-forward days and look at Unpaid and a zero-balance type.

**Acceptance Scenarios**:

1. **Given** Unpaid leave has no balance, **When** I open the dashboard, **Then** I am not shown a numeric balance for Unpaid.
2. **Given** I have Annual carry-forward days that expire on 31 March, **When** I open the dashboard, **Then** I can tell how many of my available days are carry-forward and when they expire.
3. **Given** a leave type where I have 0 days accrued, **When** I open the dashboard, **Then** it shows 0 available and not an error.

---

### Edge Cases

- A balance can never be negative, so available never shows below 0.
- A half-day pending request counts 0.5 in pending.
- A new joiner has a pro-rata Annual amount, rounded to the nearest 0.5. The dashboard shows that amount as accrued.
- Fractions are shown exactly (for example 7.5), never rounded to whole days.
- If balances cannot be loaded, the dashboard shows a clear error for that area and does not show stale or zero figures as if they were real.
- A person sees only their own balances, never someone else's.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST show a signed-in person, on their dashboard, a balance for each leave type that has a balance.
- **FR-002**: For each type the system MUST show accrued, used, pending and available days, where available = accrued - used - pending.
- **FR-003**: Pending MUST equal the total days of the person's own requests that are awaiting approval, and MUST drop when a request is approved (it becomes used), rejected or cancelled.
- **FR-004**: Days MUST count as pending from the moment a request is submitted (days are deducted at submit) and MUST be restored on reject or cancel.
- **FR-005**: The system MUST show half days as 0.5 and keep fractions exact.
- **FR-006**: The system MUST NOT show a numeric balance for leave types that have none (Unpaid).
- **FR-007**: The system MUST show which part of the Annual balance is carry-forward and when it expires (31 March).
- **FR-008**: A person MUST be able to see only their own balances. A request for another person's balance MUST be answered as "not found".
- **FR-009**: Available MUST never be shown below zero.
- **FR-010**: The figures MUST come from the system's stored balance records, not be calculated by the screen.

### Key Entities *(include if feature involves data)*

- **Leave balance**: One person's position in one leave type for the current leave year: accrued, used, pending, available, and the carry-forward part with its expiry date.
- **Leave type**: A configurable category of leave (seven are seeded). Says whether the type has a balance.
- **Leave request (pending)**: A submitted request awaiting a decision. Its day count is the source of the pending figure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A signed-in person sees all their balances within 2 seconds of opening the dashboard.
- **SC-002**: Both Jira examples (8 available and 0 pending; 5 available with 3 pending) show the expected figures in a verification run.
- **SC-003**: In a check of balances after submit, reject and cancel, available and pending are correct in every case.
- **SC-004**: A person can state how many days of each leave type they can take without leaving the dashboard.

## Assumptions

- Sign-in (US-01 / SCRUM-8) is in place, so the dashboard knows who the person is.
- Balances and accrual records (increment 4f) and leave requests (4d) provide the data this screen reads. If they are not built yet, this story depends on them or on seeded demo balances.
- "Current leave year" is the year the balance is shown for. Showing past years is out of scope.
- Only the signed-in person's own balances are shown. Managers viewing a team's balances is out of scope.
- Editing or adjusting balances is out of scope (HR work in 4f).
- The dashboard is the only place this story adds. A separate balances page is out of scope.
- Pending counts only the person's own requests awaiting approval.
