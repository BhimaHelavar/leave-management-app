# Feature Specification: Approve or reject a request with a comment

**Feature Branch**: `SCRUM-14-US-07-Approve-or-reject-a-request-with-a-comment`

**Created**: 2026-11-14

**Status**: Draft

**Input**: User description: "read jira story form https://bhimashankarcse92.atlassian.net/browse/SCRUM-14" (Jira SCRUM-14, US-07, requirements BR-05 and BR-06 Must, epic SCRUM-7, labels leave-mgmt and vertical-slice, priority High, sprint "SCRUM Sprint 5"; updated after a role-play to add the email deep link)

> Story: **As a** manager, **I want to** approve or reject with a comment **so that** the employee knows the outcome and why.
> Acceptance: (1) Given a pending request, when I reject without a comment, then it is blocked. (2) When I approve, then status becomes Approved, balance is deducted, and the employee is emailed. (3) Given I open the deep link from the notification email, then I land directly on that request with Approve and Reject buttons (after sign-in).

## Scope note

This is the first story that **changes** a request after it is submitted (increment 4e). It builds on US-03 (apply), US-05 (blocking rules) and US-06 (the manager inbox). Cancelling a request, HR approving on behalf of an absent manager (policy rule 14) and delegation are separate stories and are out of scope here.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reject a request with a comment (Priority: P1)

A signed-in manager opens a pending request from one of their direct reports and rejects it, writing a comment that explains why. The request becomes Rejected, the days held for it go back to the employee's balance, the dates become free again, and the comment is kept with the decision so the employee can read it.

**Why this priority**: A rejection without a reason is the situation the story exists to prevent, and the days must come back or the employee loses leave they never used.

**Independent Test**: As a manager, reject a pending request of a direct report with a comment and check the status is Rejected, the employee's available balance is higher by the request's days, and the employee can apply again for the same dates.

**Acceptance Scenarios**:

1. **Given** a pending request from my direct report, **When** I reject it without a comment, **Then** it is refused, the request stays pending and nothing changes.
2. **Given** a pending request, **When** I reject it with a comment made only of spaces, **Then** it is refused in the same way.
3. **Given** a pending request, **When** I reject it with a comment, **Then** the status becomes Rejected and my comment is saved with the decision.
4. **Given** a rejected request of 3 days, **When** I look at the employee's balance, **Then** available is 3 days higher than before the rejection and the 3 days are no longer pending.
5. **Given** a rejected request, **When** the employee applies for the same dates again, **Then** the request is accepted (the dates are free).
6. **Given** a rejection, **When** it is saved, **Then** an audit record of the decision is written with the same save.
7. **Given** a comment longer than 500 characters, **When** I reject, **Then** it is refused with a clear message and nothing changes.

---

### User Story 2 - Approve a request, with an optional comment (Priority: P1)

A signed-in manager approves a pending request of a direct report. The status becomes Approved. The days stay counted as used; no further days are taken. A comment is optional.

**Why this priority**: Approving is the main purpose of the inbox, and the balance must stay correct.

**Independent Test**: Approve a pending request and check the status is Approved, the employee's available balance is unchanged, and the days now count as used instead of pending.

**Acceptance Scenarios**:

1. **Given** a pending request, **When** I approve it with no comment, **Then** the status becomes Approved.
2. **Given** a pending request, **When** I approve it with a comment, **Then** the status becomes Approved and the comment is saved.
3. **Given** an approved request of 3 days, **When** I look at the employee's balance, **Then** available is the same as before the approval, the 3 days are no longer pending, and they show as used.
4. **Given** an approval, **When** it is saved, **Then** an audit record of the decision is written with the same save.
5. **Given** the request is approved, **When** the employee applies for overlapping dates, **Then** the request is still refused as an overlap (an approved request holds its days).

---

### User Story 3 - Only the right manager can decide, and only once (Priority: P1)

Only the manager of the employee can decide on a request. A manager cannot decide on their own request, on a request of someone who is not their report, or on a request that has already been decided. Two decisions at the same moment cannot both succeed.

**Why this priority**: It is a security and data rule. Letting the wrong person decide, or deciding twice, would corrupt balances and the audit trail.

**Independent Test**: Try to decide a request as an Employee, as a different manager, as the requester themself, and twice in a row. Each attempt is refused and changes nothing.

**Acceptance Scenarios**:

1. **Given** I am an Employee without the Manager role, **When** I try to decide a request, **Then** I am refused.
2. **Given** a request of a person who is not my direct report, **When** I try to decide it, **Then** the answer is "not found" and nothing is revealed about the request.
3. **Given** I am a manager and the request is my own, **When** I try to decide it, **Then** it is refused (a manager never decides their own request).
4. **Given** a request that is already approved or rejected, **When** I try to decide it again, **Then** it is refused as a conflict, naming that it is no longer pending, and nothing changes.
5. **Given** two decisions on the same request sent together, **When** both arrive, **Then** exactly one succeeds and the other is refused as a conflict.
6. **Given** I am not signed in, **When** I try to decide a request, **Then** I am sent to sign in.
7. **Given** a request that has no balance effect (Unpaid), **When** I approve or reject it, **Then** the decision works and no balance changes.

---

### User Story 4 - The employee is told the outcome (Priority: P2)

When a request is decided, the employee is notified of the outcome, and can read the manager's comment. The notification refers to the request and carries a link that opens it; it holds ids only, and the comment is read from the decided request when the message is built.

**Why this priority**: The story's goal is that the employee knows the outcome and why. It matters, but the decision itself must work first.

**Independent Test**: Decide a request and check a notification for the employee is created that refers to the request and names the outcome (Approved or Rejected); the comment stays on the request and is not copied into the notification.

**Acceptance Scenarios**:

1. **Given** I approve a request, **When** it is saved, **Then** a notification for the employee is created with the outcome Approved.
2. **Given** I reject a request with a comment, **When** it is saved, **Then** a notification for the employee is created with the outcome Rejected (the comment stays on the request and is not copied into the notification).
3. **Given** the decision is refused or fails, **When** nothing is saved, **Then** no notification is created.
4. **Given** the notification cannot be delivered at once, **When** I decide, **Then** my decision still succeeds and the notification is kept to be delivered later.
5. **Given** a notification, **When** I read it, **Then** it holds no more personal data than needed (no names, emails or free text such as the manager's comment or the employee's reason, only ids and the outcome).

---

### User Story 5 - Open a request from the link and decide it (Priority: P2)

A manager who follows the link in a notification signs in if needed and lands directly on that request, which shows its details and Approve and Reject buttons. A manager may also open the same request from the inbox.

**Why this priority**: It was added after a role-play to make deciding quick, but it needs the decision to exist first.

**Independent Test**: Open the link for a pending request of a direct report while signed out. After signing in the request is shown with Approve and Reject buttons. Open the link for someone else's request and see a "not found" page.

**Acceptance Scenarios**:

1. **Given** I follow a link to a pending request of my direct report, **When** I am signed out, **Then** I am asked to sign in and then land on that request.
2. **Given** I am signed in as the manager, **When** I open the link, **Then** I see the request's details and Approve and Reject buttons.
3. **Given** the request is already decided, **When** I open the link, **Then** I see the outcome and no buttons.
4. **Given** the link is for a request I may not see, **When** I open it, **Then** I see the same "not found" page as for a request that does not exist.
5. **Given** I choose Reject, **When** I have not written a comment, **Then** the screen asks for one before sending, and the server still refuses if it is missing.
6. **Given** I decide on the screen, **When** the server accepts, **Then** I see the new status; **When** it refuses, **Then** I see the reason and the buttons stay available only if the request is still pending.

---

### Edge Cases

- The server decides. The screen only shows or hides the buttons; a decision sent straight to the server follows the same rules.
- A decision cannot be undone in this story. Cancelling an approved request is a separate story.
- A request whose start date is already past can still be decided (for example a late approval). The rules about dates were checked when it was submitted.
- The days restored on rejection are exactly the request's saved day count, never recalculated, even if holidays have changed since.
- If the employee has been deactivated or no longer reports to the manager, the manager can no longer decide the request (it is not found for them).
- A manager who holds both Manager and HR roles decides only their own reports' requests here. HR acting for an absent manager is another story.
- A comment is plain text. It is shown to the employee exactly as written and never interpreted as markup.
- Decisions leave the employee's other requests unchanged.
- The decision, the balance change, the released dates, the audit record and the notification all succeed together or none does.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A signed-in manager MUST be able to approve or reject a pending request of one of their direct reports.
- **FR-002**: The server MUST refuse a rejection with no comment, or a comment that is only spaces, and leave the request pending. A comment is limited to 500 characters; a longer one MUST be refused.
- **FR-003**: Approval MUST accept an optional comment, limited to 500 characters.
- **FR-004**: Approving MUST set the status to Approved and MUST NOT take any further days from the balance. The days already held at submit stay counted, and they move from pending to used.
- **FR-005**: Rejecting MUST set the status to Rejected, restore the request's saved day count to the employee's available balance, and release the dates so the employee can apply for them again.
- **FR-006**: A leave type with no balance (Unpaid) MUST be decided without any balance change.
- **FR-007**: Only a pending request MAY be decided. A request already approved, rejected or cancelled MUST be refused as a conflict and left unchanged.
- **FR-008**: Two decisions on the same request at the same time MUST NOT both succeed.
- **FR-009**: Only the manager of the request's owner MAY decide it. The server MUST refuse anyone without the Manager role, MUST answer "not found" for a request whose owner is not the caller's direct report, and MUST refuse a manager deciding their own request.
- **FR-010**: The relationship between manager and employee MUST be looked up when the decision is made. It MUST NOT come from the sign-in token or from anything the browser sends. The person deciding is taken from the sign-in, never from the request body.
- **FR-011**: Each decision MUST write one audit record in the same save as the status change, the balance change and the released dates. The record holds ids, the outcome and values, never names, emails or free text.
- **FR-012**: Each decision MUST create one notification for the employee in the same save, naming the outcome and holding ids only: the manager's comment stays on the request and is not copied into the notification, because notifications never hold free text (security rule 12). A decision that is refused MUST create none. Failing to deliver a notification MUST NOT undo the decision. This story only **records** the notification (owner decision, 2026-11-14). The real email is delivered by the notifications increment (4g), so acceptance criterion 2's "the employee is emailed" is met in full only when 4g is built.
- **FR-013**: The notification MUST carry a link that opens the request. The link MUST NOT contain a sign-in token or any secret.
- **FR-014**: A manager MUST be able to open a request directly from a link (and from the inbox), after signing in if needed. The page shows the request's details and, only while it is pending, Approve and Reject buttons. A request the person may not see MUST look the same as one that does not exist.
- **FR-015**: Reject on the screen MUST ask for the comment before sending. The server's rule in FR-002 still applies.
- **FR-016**: The screen MUST show the new status after a successful decision and the server's reason after a refusal. Buttons MUST be buttons, labelled, usable by keyboard, with visible loading and error states.
- **FR-017**: A comment MUST be shown as plain text and never as markup.
- **FR-018**: The decision, the balance change, the released dates, the audit record and the notification MUST succeed together or none MUST. This one transaction writes across the leave, balances, audit and notifications modules, so it is done by a single named store, the **leave decision store** (constitution Principle III, version 1.2.0, requires the spec to name each such store). It reads other modules' reference data only through ports.

### Key Entities *(include if feature involves data)*

- **Leave request**: Gains its decision: status Approved or Rejected, who decided, when, and the comment. Its saved day count is never recalculated.
- **Leave request day**: The request's held half-day slots. Rejecting releases them so the dates are free again; approving keeps them.
- **Leave balance and its ledger**: Available days per leave type and year. Rejecting restores the request's days with a ledger entry; approving changes nothing.
- **Audit record**: One append-only record per decision (ids, outcome, values).
- **Notification**: A record that the employee should be told the outcome, with the comment and a link to the request.
- **Manager and direct report**: As in US-06. The manager of the request's owner is the only person who may decide.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A rejection without a comment is refused 100% of the time and leaves the request pending and every balance unchanged.
- **SC-002**: After a rejection, the employee's available balance rises by exactly the request's saved day count and the same dates can be applied for again, in every checked case (full day, range, half day).
- **SC-003**: After an approval, available is unchanged and the days show as used instead of pending, in every checked case.
- **SC-004**: A decision by someone other than the owner's manager is refused in 100% of the checked cases (Employee, another manager, the requester, a request that does not exist), and the answer does not reveal whether the request exists.
- **SC-005**: Of two decisions sent together for one request, exactly one succeeds in every checked run.
- **SC-006**: Every successful decision has exactly one audit record and one notification, and every refused one has none.
- **SC-007**: A manager can open a request from its link and decide it in under 1 minute after signing in.

## Assumptions

- US-03 (apply), US-05 (blocking rules) and US-06 (manager inbox) are in place. Days are held (deducted) when a request is submitted, so approving does not deduct them again and rejecting restores them. The Jira wording "balance is deducted" on approval is read as "the days stay counted against the balance, and now show as used". This follows the accepted policy facts (days are deducted at submit and restored on reject or cancel).
- Cancelling a request, HR approving on behalf of an absent manager (policy rule 14), delegation and escalation are out of scope.
- A manager decides only their own direct reports' requests. HR decisions are a different story.
- A decision is final in this story. There is no undo.
- Real email delivery is out of scope (owner decision on FR-012). The notification is recorded with ids only, so that increment 4g can deliver it later; no mail library, mail server or new package is added in this story.
- The deep link opens the web app on a request page. The link carries only the request's id and no secrets; sign-in works as it does everywhere else.
- Viewing the request page needs the request's details (dates, days, balance, type, team overlap) that the inbox already shows; the manager may also see the employee's comment-free summary, never the employee's free-text reason beyond what the project's privacy rules allow.
- The release of the dates on reject is the hand-off recorded from US-03 and US-05 (rejected requests must never clash).
- Dates are plain calendar dates; "now" for the decision time comes from the system clock service.
