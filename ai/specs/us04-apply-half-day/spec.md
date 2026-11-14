# Feature Specification: Apply for a half day

**Feature Branch**: `SCRUM-11-US-04-Apply-for-a-half-day`

**Created**: 2026-11-14

**Status**: Draft

**Input**: User description: "read it from jira https://bhimashankarcse92.atlassian.net/browse/SCRUM-11" (Jira SCRUM-11, US-04, requirement BR-03 Must, epic SCRUM-7, policy rule 11)

> Story: **As an** employee, **I want to** apply for a half day **so that** I do not lose a full day.
> Acceptance: (1) Given a single date, when I choose first half, then the count is 0.5. (2) Given a multi-day range, then the half-day option is disabled.

## Relationship to US-03

US-03 (SCRUM-10, `ai/specs/us03-apply-leave-working-days`) already delivers most of this story. This spec covers only what is left, and records what is already done so nothing is built twice.

| Behaviour | Status | Where |
| --- | --- | --- |
| A single day as first or second half counts 0.5 (rule 11) | Already delivered | US-03 FR-004, scenario 4 |
| A half day on a multi-day range, a weekend or a holiday is refused by the server | Already delivered | US-03 FR-004, edge cases |
| The half-day choice is offered only for a single day | Delivered, but the choice is **hidden**, not **disabled** | US-03 apply screen |
| Acceptance criterion 2 as written ("disabled") | **Gap, covered here** | This spec |
| Both Jira criteria checked end to end for the half-day path | **Gap, covered here** | This spec |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See the half-day option disabled for a multi-day range (Priority: P1)

A signed-in person filling in the apply form who has chosen a start date and an end date on different days still sees the half-day choice, but it is disabled and shows a short hint that a half day needs a single day. When the dates become one day again, the choice is enabled again.

**Why this priority**: It is the one acceptance criterion the current screen does not meet as written. A disabled option also tells the person why half days are unavailable, which a hidden option does not.

**Independent Test**: Open the apply form, pick two different dates, and check the half-day choice is visible, disabled and explained. Pick the same date for both and check it is enabled.

**Acceptance Scenarios**:

1. **Given** I have chosen a start date and a later end date, **When** I look at the form, **Then** the half-day choice is shown but disabled, with a hint that a half day applies to a single day only.
2. **Given** the half-day choice is disabled, **When** I submit, **Then** the request is sent as a full-day range and no half day is applied.
3. **Given** I had chosen a first or second half on a single date, **When** I change the end date to a later day, **Then** the choice goes back to full day and becomes disabled.
4. **Given** I change the end date back to the start date, **When** the form updates, **Then** the half-day choice is enabled again and starts at full day.

---

### User Story 2 - A single half day counts 0.5 (Priority: P1)

A signed-in person picks one working day and chooses first half or second half. The preview and the saved request both show 0.5 day, and 0.5 is taken from the balance.

**Why this priority**: This is the whole point of the story: the person keeps the other half of the day. It already works through US-03; this spec adds an end-to-end check so the story is proven, not assumed.

**Independent Test**: As a person with Annual balance, apply for one future working day as first half and check the preview shows 0.5, the saved request shows 0.5, and the balance drops by 0.5.

**Acceptance Scenarios**:

1. **Given** a single working day, **When** I choose first half, **Then** the preview shows 0.5 and the saved request counts 0.5.
2. **Given** a single working day, **When** I choose second half, **Then** the preview shows 0.5 and the saved request counts 0.5.
3. **Given** a request saved as a half day, **When** I view my balance, **Then** pending has risen by 0.5.
4. **Given** a single day that is a weekend or a public holiday, **When** I choose a half day, **Then** the request is refused as a day with no working time.
5. **Given** a leave type that does not allow half days, **When** I choose a half day, **Then** the request is refused with a clear message.

---

### Edge Cases

- The server stays the authority. Disabling the option on the screen is a convenience only. A request that sends a half day with a multi-day range is still refused by the server.
- Changing the dates after choosing a half day must never send a stale half-day choice.
- Whether a leave type allows half days is decided by the server (US-03). The screen does not need to know it; the refusal explains it.
- A half day and a full day on the same date overlap, so rule 13 still applies. Two half days on the same date, one first half and one second half, are outside this story (see Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When the start and end dates are the same day, the apply form MUST offer full day, first half and second half.
- **FR-002**: When the start and end dates are different days, the half-day choice MUST be shown disabled, not hidden, with a short hint that a half day applies to a single day only.
- **FR-003**: When the dates change from one day to a range, any chosen half day MUST be reset to full day. When they change back to one day, the choice MUST start at full day.
- **FR-004**: A request submitted while the half-day choice is disabled MUST be sent as a full-day request.
- **FR-005**: A single working day chosen as first or second half MUST count 0.5, in the preview and in the saved request (rule 11). This is delivered by US-03 FR-004 and MUST keep working.
- **FR-006**: The server MUST keep refusing a half day on a multi-day range, on a weekend or holiday, or for a leave type that does not allow it, whatever the screen sent (US-03 FR-004, FR-020).
- **FR-007**: The hint and the disabled state MUST be understandable without colour alone, and the disabled choice MUST be reachable by assistive technology together with its hint.

### Key Entities *(include if feature involves data)*

- **Leave request**: Unchanged from US-03. Its half-day field is none, first half or second half, and its count is a snapshot.
- **Leave type**: Unchanged from US-03. It says whether half days are allowed.

No new data is introduced.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a check of the apply form, for every date pair that spans more than one day, the half-day choice is visible and disabled with its hint in 100% of cases, and enabled for every single-day pair.
- **SC-002**: A person can apply for one first-half day and one second-half day (on different dates) and each is saved as exactly 0.5.
- **SC-003**: A half day sent with a multi-day range is refused 100% of the time, even when the screen is bypassed.
- **SC-004**: Both Jira acceptance criteria pass in one verification run, with the result reported.

## Assumptions

- US-03 (SCRUM-10) is merged. This story builds on its apply screen, preview and server rules and does not change any server rule.
- No back-end behaviour changes are expected. If a gap is found there, this spec is updated and approved again before any code.
- Only one half-day request per date is in scope. Booking a first half and a second half on the same date as two requests would conflict with the overlap rule (rule 13) and needs its own story.
- Viewing the list of my requests is out of scope (increment 4d).
- "Disabled" means visible, not selectable and explained, as the Jira criterion states.
- Which leave types allow half days is data set per type, as in US-03.
