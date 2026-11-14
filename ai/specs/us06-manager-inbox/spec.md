# Feature Specification: Review pending requests in a manager inbox

**Feature Branch**: `SCRUM-13-US-06-Review-pending-requests-in-a-manager-inbox`

**Created**: 2026-11-14

**Status**: Draft

**Input**: User description: "read the entire https://bhimashankarcse92.atlassian.net/browse/SCRUM-13 stories and write speck for each story supprately" (Jira SCRUM-13 is one story, US-06, requirement BR-05 Must, epic SCRUM-7, label vertical-slice, priority High, sprint "SCRUM Sprint 5"; BR-11 team calendar stays a Could)

> Story: **As a** manager, **I want to** review pending requests in an inbox **so that** I can decide quickly.
> Acceptance: (1) Given 3 pending requests from my reports, when I open the inbox, then I see 3 items with dates, count, balance after approval and any team overlap. (2) Inbox is sorted by start date, soonest first. (3) Given 2 of my 5 reports are approved or pending for the same dates, then each request shows who else is out and how many of the team would be away.

## Scope note

SCRUM-13 holds one story (US-06) and no child stories, so this is the only spec for it. This story is **read only**: the manager sees the inbox. Deciding on a request (approve, reject, comment, audit) is a separate story in increment 4e and is out of scope here.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See my reports' pending requests in one list (Priority: P1)

A signed-in manager opens the inbox and sees every pending leave request from their direct reports. Each item shows who asked, the leave type, the start and end dates, the number of days, and the person's balance after this request. The list is sorted by start date, soonest first.

**Why this priority**: This is the whole story. Without the list a manager has nothing to decide on.

**Independent Test**: Sign in as a manager with 3 direct reports who each have one pending request. Open the inbox and check 3 items appear, each with dates, day count and balance, in start-date order.

**Acceptance Scenarios**:

1. **Given** 3 pending requests from my direct reports, **When** I open the inbox, **Then** I see 3 items, each showing the person, leave type, start date, end date, day count and balance after this request.
2. **Given** pending requests starting on different dates, **When** I open the inbox, **Then** the item with the earliest start date is first.
3. **Given** two requests with the same start date, **When** I open the inbox, **Then** their order is stable and the same every time (earlier submitted first).
4. **Given** a report's request is approved, rejected or cancelled, **When** I open the inbox, **Then** it is not listed (only pending requests appear).
5. **Given** none of my reports has a pending request, **When** I open the inbox, **Then** I see a clear "nothing pending" message, not an error and not a blank page.
6. **Given** the inbox cannot be loaded, **When** I open it, **Then** I see an error with a way to try again.
7. **Given** a request is half a day, **When** I see its item, **Then** the day count shows 0.5 and says first or second half.

---

### User Story 2 - See who else is away for the same dates (Priority: P1)

For each request, the manager sees which of their other direct reports are out on any of the same days (pending or approved leave), and how many of the team would be away if this request were approved. This lets the manager protect team cover without opening a calendar.

**Why this priority**: The story was updated after a role-play specifically to add this, and it is the main reason to decide from one screen.

**Independent Test**: With 5 reports, give 2 of them pending leave on the same dates. Open the inbox and check each of the two requests names the other and shows the team-away figure.

**Acceptance Scenarios**:

1. **Given** 2 of my 5 reports have pending or approved requests on the same dates, **When** I open the inbox, **Then** each of those two requests names the other person and shows how many of the team would be away.
2. **Given** a request has no overlap with any other report, **When** I see its item, **Then** it says no one else in the team is away.
3. **Given** an overlap is only partial (one shared day out of five), **When** I see the item, **Then** the other person is still named and the shared dates are shown.
4. **Given** another report has an approved request on those dates, **When** I see the item, **Then** that person counts as away, the same as for a pending one.
5. **Given** another report's request is rejected or cancelled, **When** I see the item, **Then** that person is not counted as away.
6. **Given** a person who is not my direct report is away on the same dates, **When** I see the item, **Then** they are not named or counted.

---

### User Story 3 - See only my own reports' requests (Priority: P1)

A manager sees requests from their direct reports only. A person who is not a manager cannot open the inbox. A manager never sees another manager's reports, and never sees their own requests there.

**Why this priority**: It is a security rule (the server decides who may see which record), and a leak here would expose other people's leave.

**Independent Test**: Sign in as a manager with 2 reports while another manager has a different report with a pending request. Check the other report's request never appears, and check an Employee-only user is refused.

**Acceptance Scenarios**:

1. **Given** another manager's report has a pending request, **When** I open my inbox, **Then** it does not appear.
2. **Given** I am a manager and I have my own pending request, **When** I open my inbox, **Then** it does not appear (a manager never approves their own request).
3. **Given** I am an Employee with no Manager role, **When** I try to open the inbox, **Then** I am refused and nothing is listed.
4. **Given** I am not signed in, **When** I try to open the inbox, **Then** I am sent to sign in.
5. **Given** I hold both Manager and HR roles, **When** I open the inbox, **Then** I still see only my direct reports' requests (the HR escalation list is a different story).
6. **Given** a request is for a report who no longer reports to me, **When** I open the inbox, **Then** it does not appear.

---

### Edge Cases

- The server decides who sees what. The screen only shapes the list; a call made straight to the server returns the same result.
- A manager with no direct reports sees the "nothing pending" message.
- The team is the manager's direct reports. The requester is part of the team, so a team of 5 means 5 people including the requester.
- Overlap is checked on the working days of the request and the other person's held days, so a weekend inside a range is never an overlap.
- A half day overlaps a full day on the same date, but a first half does not overlap a second half on the same date.
- A request can start in the past only if it was already pending when the date passed; it still appears, sorted first.
- A very long list is shown a page at a time, with the soonest start dates on the first page.
- A person can have more than one pending request. Each is its own item.
- The inbox reflects changes when reopened or refreshed. A request decided in another window disappears on the next refresh.
- Leave reasons (free text) are not shown in this list.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A signed-in manager MUST be able to open an inbox listing the pending leave requests of their direct reports.
- **FR-002**: Each item MUST show the person's name, the leave type, the start date, the end date, the day count (0.5 for a half day, with first or second half), and the person's balance after this request.
- **FR-003**: The inbox MUST list only requests that are pending. Approved, rejected and cancelled requests MUST NOT appear.
- **FR-004**: The inbox MUST be sorted by start date, soonest first. Requests with the same start date MUST have a fixed order (earlier submitted first).
- **FR-005**: For each item the inbox MUST show which of the manager's other direct reports have a pending or approved request on any of the same working days, naming each person and the shared dates.
- **FR-006**: For each item the inbox MUST show how many of the team would be away if the request were approved, out of the team size. The count includes the person who made the request, so two reports out on the same dates show "2 of 5 away" on both requests (owner decision, 2026-11-14; the S-06 sketch's "(1 of 5 out)" counted others only and is superseded here).
- **FR-007**: The inbox MUST show only requests of the signed-in manager's direct reports. It MUST NOT show the manager's own requests, another manager's reports, or people who no longer report to the manager.
- **FR-008**: The server MUST refuse anyone without the Manager role. A person who is not signed in MUST be sent to sign in. A holder of Manager and HR roles MUST still see only direct reports.
- **FR-009**: The manager's relationship to a person MUST be looked up when the inbox is read. It MUST NOT be taken from the sign-in token or from anything the browser sends.
- **FR-010**: When nothing is pending the inbox MUST show a clear empty message. When loading fails it MUST show an error with a way to try again. While loading it MUST show a visible loading state.
- **FR-011**: The inbox MUST show names only to the manager of the person. It MUST NOT show leave reasons, other people's balances beyond the requester's, or any person outside the manager's team.
- **FR-012**: The inbox MUST be readable by keyboard and screen reader: each item has labelled fields and the overlap text does not depend on colour.
- **FR-013**: Reading the inbox MUST NOT change any request, balance or audit record.
- **FR-014**: A long list MUST be shown a page at a time, with the soonest start dates first, and the manager MUST be able to reach the next page.

### Key Entities *(include if feature involves data)*

- **Pending request**: A leave request in the pending state, belonging to one person. Has dates, a day count (a saved snapshot), a leave type and a submitted time.
- **Direct report**: A person whose manager is the signed-in manager. The manager's team is all of their direct reports.
- **Team overlap**: For one request, the other direct reports who hold pending or approved leave on any of its working days, with the shared dates, and the team-away figure.
- **Balance after**: The requester's available days for that leave type once this request's days are counted. Days are already held at submit, so this is the person's available balance now and the figure before the request is that plus the request's days.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A manager with 3 pending requests from reports sees exactly 3 items, each with all required fields, in start-date order, in 100% of the checked cases.
- **SC-002**: With 5 reports and 2 of them out on the same dates, each of the two requests names the other and shows the team-away figure, in 100% of the checked cases.
- **SC-003**: In every check with several managers, no request of another manager's report and no request of the manager themself ever appears.
- **SC-004**: An Employee-only user is refused 100% of the time, and the refusal does not reveal whether anyone has pending requests.
- **SC-005**: A manager sees the inbox, with up to 20 items, within 2 seconds of opening it.
- **SC-006**: Opening the inbox leaves every balance, request and audit record exactly as it was.

## Assumptions

- Sign-in (US-01, SCRUM-8) and apply (US-03, SCRUM-10) are in place, so pending requests exist. The manager relationship is already stored on each person.
- Approving, rejecting and cancelling are out of scope. This story only reads. Those actions belong to increment 4e and will add buttons to the same list later.
- HR's list of escalations and requests with no manager (policy rule 14) is a different story. A person with no manager does not appear in any inbox in this story.
- The team is the manager's direct reports only. A wider team calendar is BR-11 (Could) and is out of scope.
- "Away" means a pending or approved request, as the Jira story says. Rejected and cancelled requests do not hold days, so they never count.
- "Balance after approval" is shown as the requester's available days after this request is counted. Because days are held when the request is submitted, approving does not change the number; the item shows the figure before the request and the figure after, for example 8.0 to 5.0.
- The list follows the project's list conventions (page size 20 by default, 100 at most, soonest first).
- The manager sees names of their own reports. Employees seeing team overlap is a different question (open question 4 in the API design) and is not part of this story.
- Dates are plain calendar dates with no time of day. "Now" and the pending state come from stored data, not from the screen.
- Increment 4e must keep this inbox correct: a decided request must leave the pending state in the same step as the decision.
