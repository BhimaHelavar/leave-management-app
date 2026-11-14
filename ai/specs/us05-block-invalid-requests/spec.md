# Feature Specification: Block requests that break my balance or overlap

**Feature Branch**: `SCRUM-12-US-05-Block-requests-that-break-my-balance-or-overlap`

**Created**: 2026-11-14

**Status**: Draft

**Input**: User description: "read jira from https://bhimashankarcse92.atlassian.net/browse/SCRUM-12" (Jira SCRUM-12, US-05, requirements BR-03 and BR-10, epic SCRUM-7, policy rules 5 and 13, label vertical-slice, priority High)

> Story: **As an** employee, **I cannot** apply beyond my rules **so that** invalid requests never reach my manager.
> Acceptance: (1) Given 2 Annual available, when I request 3 days, then I see "insufficient balance" and nothing is saved. (2) Given an overlap with an approved request, then it is rejected naming the clashing dates.

## Relationship to US-03

US-03 (SCRUM-10, `ai/specs/us03-apply-leave-working-days`, merged in PR #5) already delivers most of this story. This spec covers only what is left, and records what is already done so nothing is built twice.

| Behaviour | Status | Where |
| --- | --- | --- |
| A request that needs more days than the available balance is refused and nothing is saved (rule 5) | Already delivered | US-03 FR-011, FR-014, scenario 4 of its story 3 |
| A request that overlaps the person's own pending or approved request is refused and nothing is saved (rule 13) | Already delivered | US-03 FR-010, FR-014 |
| Two submits that race each other cannot both succeed | Already delivered | US-03 FR-017 and SC-005 |
| The refusal says "insufficient balance" in plain words | Already delivered | US-03 FR-020 wording table |
| The overlap refusal **names the clashing dates** | **Gap, covered here** | Today the message only says "one or more of these days" |
| Both Jira criteria checked end to end | **Gap, covered here** | This spec |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Be told which dates clash (Priority: P1)

A signed-in person applies for leave that overlaps a pending or approved request of their own. The request is refused, nothing is saved, and the message names the dates that clash, so they can see what to change without opening their request list.

**Why this priority**: It is the one acceptance criterion the current system does not meet as written. A message that only says "one or more days" sends the person hunting for the clash.

**Independent Test**: Hold a pending request for Tue 1 Dec to Wed 2 Dec. Apply for Mon 30 Nov to Wed 2 Dec. Check the refusal names 1 Dec and 2 Dec (and not 30 Nov), and that balances and requests are unchanged.

**Acceptance Scenarios**:

1. **Given** I have an approved or pending request for Tue 1 Dec to Wed 2 Dec, **When** I apply for Mon 30 Nov to Wed 2 Dec, **Then** the request is refused and the message names 1 Dec and 2 Dec as the clashing dates.
2. **Given** my existing request covers a single day, **When** I apply for a range that includes that day, **Then** the message names that one date.
3. **Given** the clash is with a request that is approved, **When** I apply, **Then** the refusal is the same as for a pending one (rule 13 treats both alike).
4. **Given** the range clashes with two separate requests, **When** I apply, **Then** the message names the clashing dates inside my range in date order (up to 10; if there are more, it names the first 10 and says how many more).
5. **Given** a refusal for overlap, **When** I look at my balance and my requests, **Then** nothing has changed.
6. **Given** a request of mine that is rejected or cancelled covers some of the dates, **When** I apply for those dates, **Then** it is not treated as a clash.

---

### User Story 2 - Be told the balance is too low, with nothing saved (Priority: P1)

A signed-in person asks for more days than they have available. The request is refused with a plain "insufficient balance" message, and nothing is saved or deducted.

**Why this priority**: It is the other half of the story and proves the guarantee ("invalid requests never reach my manager"). It already works through US-03; this spec adds an end-to-end check so it is proven, not assumed.

**Independent Test**: As a person with 2 Annual days available, apply for 3 working days. Check the refusal says there is not enough balance, no request exists, and available is still 2.

**Acceptance Scenarios**:

1. **Given** 2 Annual days available, **When** I request 3 working days, **Then** I see an "insufficient balance" message and no request is saved.
2. **Given** that refusal, **When** I check my balance, **Then** available, pending and used are unchanged.
3. **Given** exactly 3 Annual days available, **When** I request 3 working days, **Then** it is accepted (the block is for going below zero, not for reaching zero).
4. **Given** a leave type with no balance (Unpaid), **When** I request any number of days, **Then** the balance check does not apply.
5. **Given** a request breaks both the overlap rule and the balance rule, **When** I apply, **Then** the refusal names the overlap first, as the fixed rule order says, and nothing is saved.

---

### Edge Cases

- The server stays the authority. The screen only shows the refusal; a request sent straight to the server is refused in the same way.
- Two near-simultaneous submits for the same dates. **Same retry key and same request** (a double click): one request is saved and both get the same saved answer, as US-03 FR-017 says. **Different keys**: one is saved and the other is refused as an overlap, naming the clashing dates when they can be read at that moment, and otherwise with the general overlap wording. This is already guaranteed by US-03 and must keep holding.
- Only the person's own requests count. A teammate's leave is never a clash and is never named.
- The clashing dates named are only those inside the range being applied for, not the whole of the other request.
- A half day on a date that already holds a full-day request clashes; naming that date is enough.
- A date in the message is a plain calendar date. It carries no names, reasons or other people's data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST refuse a request whose days exceed the person's available balance for that leave type, save nothing, and tell the person in plain words that the balance is insufficient (rule 5). This is delivered by US-03 FR-011 and MUST keep working.
- **FR-002**: A request that uses exactly the available balance MUST be accepted. A leave type with no balance (Unpaid) MUST skip the balance check.
- **FR-003**: The system MUST refuse a request that overlaps any of the person's own pending or approved requests, and save nothing (rule 13). This is delivered by US-03 FR-010 and MUST keep working.
- **FR-004**: The overlap refusal MUST name the dates that clash, in date order, limited to the dates inside the requested range. It names at most 10 dates; when more clash it names the first 10 and says how many more. If no clashing date can be read at that moment (a rare race), it uses the general overlap wording.
- **FR-005**: Rejected and cancelled requests MUST NOT count as a clash. **Deferred to increment 4e:** this needs the reject and cancel actions to release the request's days, which are not built yet. It cannot be tested in this story. The 4e spec must carry this duty and its test (see the 4e row in `ai/specs/INDEX.md`).
- **FR-006**: The clashing dates MUST be shown to the person on the apply screen in the refusal message.
- **FR-007**: Every refusal MUST leave balances, requests and day rows unchanged (US-03 FR-014).
- **FR-008**: When several rules fail, the refusal MUST follow the fixed order already defined in US-03 (overlap before balance).
- **FR-009**: The message MUST contain only calendar dates of the person's own requests. It MUST NOT contain names, reasons or any other person's data.
- **FR-010**: Naming the dates MUST NOT weaken the guarantee that two simultaneous submits cannot both succeed (US-03 FR-017).

### Key Entities *(include if feature involves data)*

- **Leave request**: Unchanged from US-03. Its status (pending, approved, rejected, cancelled) decides whether it can clash.
- **Leave request day**: Unchanged from US-03. One slot of one working day; used to find which dates clash.
- **Leave balance**: Unchanged from US-03. Available days per leave type for the leave year; the balance rule reads it.

No new data is introduced.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With 2 Annual days available, a request for 3 working days is refused 100% of the time with an "insufficient balance" message, and available stays 2.
- **SC-002**: For an overlap with an existing request, the refusal names exactly the clashing dates inside the range in 100% of cases across 5 checked ranges: start clash, end clash, single day, a new range that contains an existing request, and a range that clashes with two separate requests.
- **SC-003**: After every refusal in these checks, the balance and the list of requests are identical to before.
- **SC-004**: Two submits for the same dates made together result in exactly one saved request. With the same retry key both calls return that one saved answer; with different keys the other call is refused as an overlap (naming the dates when they can be read).
- **SC-005**: Both Jira acceptance criteria pass in one verification run, with the result reported.

## Assumptions

- US-03 (SCRUM-10) is merged. This story builds on its apply endpoint, refusal rules and apply screen.
- Approving requests is not built yet (increment 4e). An overlap with an **approved** request therefore cannot be created through the app today. Pending requests are used to check the overlap rule, and approved ones are covered by the same rule in the same code; the test run says so plainly.
- The balance refusal text may stay as it is. Only the overlap message changes.
- Dates are listed in date order, as plain calendar dates, in the same style the screen already uses.
- The preview (live count) does not check overlap or balance, as in US-03. Showing those earlier, while typing, is out of scope for this story and would need its own decision.
- Viewing the list of my requests is out of scope (increment 4d).
- The existing refusal codes stay the same. If naming the dates needs an extra field in the refusal, that is a design question for the plan and must not break the screen's existing use of the code.
- Only the person's own requests are read. No other person's leave is ever consulted or shown.
