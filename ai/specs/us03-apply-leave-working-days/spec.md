# Feature Specification: Apply for leave with an accurate working-day count

**Feature Branch**: `SCRUM-10-US-03-Apply-for-leave-with-an-accurate-working-day-count`

**Created**: 2026-11-13

**Status**: Draft

**Input**: User description: "read jira from https://bhimashankarcse92.atlassian.net/browse/SCRUM-10" (Jira SCRUM-10, US-03, requirement BR-03 Must, epic SCRUM-7, policy rules 7, 8, 10, 13)

> Story: **As an** employee, **I want to** apply for leave with an accurate day count **so that** my balance is correct.
> Acceptance: (1) Given Fri 3 Oct to Tue 7 Oct and no holidays, when I apply, then the count is 3 working days. (2) Given a holiday on Mon 6 Oct in my location, then the count is 2.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Apply for leave and get the right working-day count (Priority: P1)

A signed-in person picks a leave type, a start date and an end date (and, for a single day, optionally a half day), and submits. The system counts only the working days in that range. Weekends and the public holidays of the person's location are not counted. The request is saved as pending with that count, and the days are taken from the person's available balance.

**Why this priority**: This is the whole story. A wrong count gives a wrong balance, which is the harm the story exists to prevent.

**Independent Test**: Sign in as a person with enough Annual balance, apply Fri 3 Oct to Tue 7 Oct, and check the saved request shows 3 days and the balance drops by 3.

**Acceptance Scenarios**:

1. **Given** no holidays between Fri 3 Oct and Tue 7 Oct, **When** I apply for that range, **Then** the count is 3 working days (Fri, Mon, Tue).
2. **Given** a holiday on Mon 6 Oct in my location, **When** I apply for Fri 3 Oct to Tue 7 Oct, **Then** the count is 2.
3. **Given** a range that includes a weekend, **When** I apply, **Then** Saturday and Sunday are not counted, and they are not counted even when they sit between two leave days (sandwich rule is off).
4. **Given** I choose a single day as a first-half or second-half day, **When** I apply, **Then** the count is 0.5.
5. **Given** my request is accepted, **When** I view my balance, **Then** the counted days show as pending and available has dropped by the same amount.
6. **Given** the range holds no working days (for example Sat to Sun), **When** I apply, **Then** the request is refused with a clear message.

---

### User Story 2 - See the count before I submit (Priority: P2)

While filling the form, the person sees the working-day count for the dates chosen, so they know what will be deducted. The count shown is a preview only; the system recounts when the request is submitted and that figure is the one saved.

**Why this priority**: It helps the person but is not needed for a correct saved count.

**Independent Test**: Change the dates on the form and check the preview matches the count saved after submit.

**Acceptance Scenarios**:

1. **Given** I have chosen a leave type and dates, **When** the form shows the preview, **Then** it equals the count the system saves on submit.
2. **Given** I change a date, **When** the form updates, **Then** the preview changes to match.

---

### User Story 3 - Be stopped when the request breaks a policy rule (Priority: P1)

The system refuses a request that breaks a rule, says which rule, and changes nothing.

**Why this priority**: The story names rules 7, 8, 10 and 13. Without enforcing them the count is accurate but the request may still be invalid.

**Independent Test**: Submit one request per rule below and check each is refused with its own clear message and leaves the balance unchanged.

**Acceptance Scenarios**:

1. **Given** I apply for Annual leave starting in fewer than 3 working days, **When** I submit, **Then** it is refused for short notice (rule 7).
2. **Given** I apply for more than 15 consecutive Annual days, **When** I submit, **Then** it is refused and I am told longer leave needs HR (rule 8).
3. **Given** I already have a pending or approved request on any day in my range, **When** I submit, **Then** it is refused as overlapping (rule 13).
4. **Given** the days needed exceed my available balance, **When** I submit, **Then** it is refused and the balance stays at or above zero (rule 5).
5. **Given** my range runs across two leave years, **When** I submit, **Then** it is refused and I am told to submit one request per year.
6. **Given** the end date is before the start date, **When** I submit, **Then** it is refused.
7. **Given** the start date is before today in my location, **When** I submit, **Then** it is refused for every leave type.

---

### Edge Cases

- The count is a snapshot saved with the request. It is never recalculated later, even if holidays change.
- A holiday that falls on a weekend does not reduce the count twice.
- Rule 8 counts consecutive calendar days of leave in the range for a leave type that has a maximum (Annual is the seeded example); weekends inside the range are part of the stretch. The exact measure is stated as an assumption below.
- A half day applies to a single day only. A half day on a weekend or holiday is refused as a day with no working time.
- Two near-simultaneous submits for the same dates must not both succeed (overlap and balance must hold under a retry or double click).
- A person can apply only for themselves. A start date before today is refused for every leave type (FR-019).
- Unpaid has no balance, so the balance check does not apply, but every other rule does.
- The saved count, the balance change and the audit record all succeed together or none do.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A signed-in person MUST be able to apply for leave by choosing a leave type, a start date, an end date, and optionally a half day (first or second half) for a single day, plus an optional comment.
- **FR-002**: The system MUST count only working days in the range. Saturdays, Sundays and the public holidays of the person's location are not counted.
- **FR-003**: The system MUST NOT count weekends or holidays that sit between leave days. The sandwich rule is off (rule 10).
- **FR-004**: A half day MUST count 0.5 and apply to a single day only (rule 11).
- **FR-005**: The count MUST be decided by the system at submit. A figure sent by the screen MUST be ignored.
- **FR-006**: The saved request MUST keep the count as a snapshot that is never recalculated.
- **FR-007**: On a successful submit the request MUST be saved as pending and the counted days MUST be taken from the person's available balance at once.
- **FR-008**: The system MUST refuse a request with less notice than its leave type requires (3 working days for Annual, 0 for the other seeded types) (rule 7).
- **FR-009**: The system MUST refuse a request longer than its leave type's maximum consecutive days (15 for Annual, no limit for the other seeded types), and tell the person that longer leave needs HR (rule 8).
- **FR-010**: The system MUST refuse a request that overlaps any of the person's own pending or approved requests (rule 13).
- **FR-011**: The system MUST refuse a request that would take a balance below zero (rule 5). Unpaid has no balance.
- **FR-012**: The system MUST refuse a request that crosses a leave year, and say that one request is needed per year.
- **FR-013**: The system MUST refuse a range with no working days and a range whose end is before its start.
- **FR-014**: Each refusal MUST change no balance or request.
- **FR-015**: The request MUST belong to the signed-in person. A person MUST NOT be able to apply on behalf of someone else or set the status, the day count or the owner.
- **FR-016**: A successful apply MUST write an audit record in the same transaction as the request and the balance change.
- **FR-017**: Submitting the same request twice (retry or double click) MUST NOT create two requests or deduct twice.
- **FR-018**: The form MUST show a preview of the working-day count (User Story 2). The preview MUST use the same counting as the submit.
- **FR-019**: The system MUST refuse a request whose start date is before today in the person's location, for every leave type.
- **FR-020**: Each refusal message MUST name the broken rule in plain words. The wording for each refusal is listed in `contracts/leave-requests-api.md`.

### Key Entities *(include if feature involves data)*

- **Leave request**: One person's application: leave type, start date, end date, optional half day, comment, status (pending on submit), and the day count snapshot.
- **Leave request day**: One half-day slot (morning or afternoon) of a working day in a request. A full working day has two slots, a half day has one. The request's count is the number of slots times 0.5.
- **Holiday**: A public holiday date for a location. Used only to decide which days are not working days.
- **Leave balance**: The person's accrued, used and pending days per leave type for the leave year. Submit moves days into pending.
- **Leave type**: The category chosen. Says whether it has a balance and which rules apply (for example Annual notice and maximum).
- **Location**: The office whose holidays apply to the person (MVP: Bengaluru).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Both Jira examples give the expected counts in a verification run: 3 working days with no holiday, and 2 with a Monday holiday.
- **SC-002**: In a check of at least 10 date ranges that mix weekends, holidays, half days and year ends, the saved count matches the hand-counted figure in every case.
- **SC-003**: A person can apply for a leave and see the updated pending figure within 3 seconds of submitting.
- **SC-004**: For each of rules 5, 7, 8 and 13, a request that breaks it is refused 100% of the time, with a message naming the rule, and the balance is unchanged.
- **SC-005**: Two identical submits made together result in exactly one request and one deduction.

## Assumptions

- Sign-in (US-01 / SCRUM-8) is in place so the system knows who the person is, and the dashboard balances (US-02 / SCRUM-9) can show the pending change.
- The working week is Monday to Friday for everyone in the MVP (one office, Bengaluru).
- Holiday dates for the person's location are held in the system. If the holiday maintenance screen (increment 4f) is not built, seeded holidays are used. Managing holidays is out of scope.
- Rule 8 measures the length of the range in calendar days from start to end, including weekends and holidays inside it. The owner should confirm this in `/speckit-clarify` or `/speckit-plan`, since rule 8's wording ("maximum consecutive days") could also mean working days.
- Rule 7 notice is the number of working days strictly between today and the start date; weekends and holidays are not working days. Applying on Monday for Friday gives 3 (Tue, Wed, Thu) and is allowed for Annual; applying on Tuesday for Friday gives 2 and is refused.
- Back-dated requests (start date before today) are refused for every leave type (FR-019). Allowing them for some types, for example HR entering sick leave after the fact, is a later decision and would need its own story.
- Rule 9 (sick leave medical certificate), rule 6 (probation) and file upload are not part of this story. They need their own stories.
- Approving, rejecting and cancelling are out of scope (4e). Viewing a list of my requests is out of scope unless needed to confirm the saved request.
- Leave days are plain dates with no time of day, and "now" comes from the system clock service.
- Rules 7 and 8 are stored per leave type, so an admin can change them. Annual is the seeded example used in the scenarios above.
- "Person", "applicant" and "employee" all mean the signed-in person applying for their own leave.
