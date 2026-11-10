# Feature Specification: Sign in and see only what my role allows

**Feature Branch**: `SCRUM-8-us01-sign-in-by-role` (not created; work stays on the current branch until the owner decides)

**Created**: 2026-11-10

**Status**: Draft

**Input**: User description: "read story from https://bhimashankarcse92.atlassian.net/browse/SCRUM-8" (Jira SCRUM-8, US-01, requirement BR-01 Must, epic SCRUM-7)

> Story: **As an** employee, **I want to** sign in **so that** I see only what my role allows.
> Acceptance: (1) Given a valid dev-mode user, when I sign in, then I land on my dashboard. (2) Given I am an Employee, when I open /admin, then I get 403 and the attempt is audited.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign in and land on my dashboard (Priority: P1)

A person who is not signed in sees a sign-in screen listing the demo people with their roles (development mode only). They pick one and arrive on their own dashboard, which shows only the sections their roles allow.

**Why this priority**: Nothing else in the product can be used or tested without a signed-in person with known roles.

**Independent Test**: Open the app signed out, pick a person, and confirm the dashboard opens and the navigation shows exactly the links for that person's roles.

**Acceptance Scenarios**:

1. **Given** I am signed out and sign-in is in development mode, **When** I open the app, **Then** I see the sign-in screen listing active demo people and their roles.
2. **Given** I am on the sign-in screen, **When** I pick a valid active person, **Then** I land on my dashboard and see my name.
3. **Given** I am signed in as an Employee only, **When** I look at the navigation, **Then** I see Dashboard and My requests and nothing else.
4. **Given** I hold more than one role (for example Employee and Manager), **When** I look at the navigation, **Then** I see the links of all my roles, without duplicates.

---

### User Story 2 - A role I do not hold is refused, and the attempt is recorded (Priority: P1)

If a signed-in person asks for something that needs a role they do not hold (for example an Employee opening the admin area), the system refuses with "forbidden" and records the attempt, so the refusal cannot be bypassed by typing a web address.

**Why this priority**: This is the second acceptance criterion of the story, and the server (not the screen) must enforce it.

**Independent Test**: Sign in as an Employee, request the admin area directly, and confirm a "forbidden" answer and a new record of the attempt.

**Acceptance Scenarios**:

1. **Given** I am signed in as an Employee, **When** I open the admin area, **Then** I get a "forbidden" (403) answer and no admin data.
2. **Given** my request was refused for a missing role, **When** an auditor reads the audit record, **Then** it shows who tried, what they tried to reach and when, and no name, email or free text.
3. **Given** I hold the required role (for example Admin), **When** I open the admin area, **Then** I get through and nothing is recorded as a refusal.

---

### User Story 3 - Signed-out and expired sessions are sent back to sign in (Priority: P2)

A person with no session, or whose session has expired or is invalid, cannot reach any protected content and is returned to the sign-in screen.

**Why this priority**: It protects every screen, but the story does not mention it directly; it closes the gap between "sign in" and "see only what my role allows".

**Independent Test**: Call a protected page without signing in, and again after the session has expired; confirm each time the person ends up on the sign-in screen.

**Acceptance Scenarios**:

1. **Given** I am not signed in, **When** I request any protected page or data, **Then** I am refused (401) and the screen shows sign-in.
2. **Given** my session has expired, **When** I take any action, **Then** I am signed out and shown sign-in.

---

### Edge Cases

- A person holds both HR and Admin: the Audit link appears once.
- A person is inactive or unknown: no session is issued.
- The list of demo people cannot be loaded: the screen says so and offers a retry.
- Sign-in is not in development mode: the people picker and the dev sign-in must not exist; the system must refuse to start in that case outside the Development or Demo environments.
- Reloading the page signs the person out (the session lives in memory only).
- A person types the address of a section their role does not allow: the server answers "forbidden"; the screen may show its not-found page, but never the section's data.
- Many sign-in attempts in a short time from one place are slowed down.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST show a sign-in screen to a signed-out person, listing active demo people and their roles, only when sign-in is in development mode.
- **FR-002**: When a valid active person is chosen, the system MUST sign them in and take them to their dashboard within the same step.
- **FR-003**: The system MUST NOT sign in an inactive or unknown person.
- **FR-004**: The system MUST treat every request as requiring a signed-in person unless it is deliberately public (health check and the dev sign-in).
- **FR-005**: The system MUST decide, on the server, which roles may use each protected feature, and MUST answer "forbidden" (403) when a signed-in person lacks the required role.
- **FR-006**: The system MUST show each person only the navigation links their roles allow, as a convenience; this MUST NOT replace FR-005.
- **FR-007**: The system MUST record every role-based refusal (FR-005) as an append-only audit entry holding who, what was attempted, and when, with no names, emails or free text.
- **FR-008**: The system MUST end the session when it expires or becomes invalid and return the person to sign-in.
- **FR-009**: The system MUST keep the session out of long-term browser storage, so a reload signs the person out.
- **FR-010**: The system MUST refuse to start in development sign-in mode outside the Development or Demo environments.
- **FR-011**: This spec depends on `4c-dev-authentication`, which stays the spec for the sign-in screen, the session token, deny-by-default and the role policies (FR-001 to FR-004, FR-008 to FR-010). This spec adds only: the refusal with 403 and its audit record (FR-005, FR-007), the role-based navigation (FR-006) and the acceptance checks of SCRUM-8.

### Key Entities *(include if feature involves data)*

- **Person (Employee)**: someone who can sign in; has a name, an email, an active flag and a manager.
- **Role**: Employee (everyone), Manager, HR or Admin; additive.
- **Session**: the proof that a person signed in; short-lived (60 minutes), carries who the person is and their roles, never their manager or other changeable facts.
- **Audit entry**: a permanent record of an attempt that was refused, holding ids and values only.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A signed-out person reaches their dashboard in no more than 2 interactions (open the app, pick a person).
- **SC-002**: For each of the four roles, the navigation shows exactly the links listed for that role and 0 links of any other role.
- **SC-003**: 100% of requests for a feature the person's roles do not allow are refused, whether or not the link was visible.
- **SC-004**: 100% of role-based refusals leave one audit record that can be found afterwards, and 0 audit records contain a name, email or free text.
- **SC-005**: 0 protected features can be reached without a valid session.

## Assumptions

- Only the development sign-in mode is in scope; Entra ID sign-in arrives later (increment 4g).
- "/admin" in the story means any area limited to Admin (for example leave types, holidays); the current shell has no literal `/admin` path, so the check is defined by role, not by that exact address.
- The server answers 403 for a role refusal (a feature the person may not use at all). For a single record the person may not see, the project rule of answering 404 still applies.
- The dashboard is the placeholder dashboard from spec T-02 until the real dashboard is built.
- The role-to-link table in `ai/specs/t02-angular-shell/contracts/navigation.md` is the source for what each role sees.
- Decided by the owner on 2026-11-10: 4c is kept as the sign-in spec (FR-011), and a minimal append-only audit writer is built in this story (FR-007); increment 4e reuses it for apply, approve, reject and cancel.
- The sign-in token lifetime is 60 minutes with no refresh, as already decided in the security rules.
