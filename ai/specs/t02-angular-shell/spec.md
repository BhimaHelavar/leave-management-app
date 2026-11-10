# Feature Specification: Application Shell, Health Check and First Deployment

**Feature Branch**: `n/a` (work stays on `main`; spec folder is `ai/specs/t02-angular-shell`)

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "read the jira from https://bhimashankarcse92.atlassian.net/browse/SCRUM-26" (T-02: Angular shell, dev auth wiring, health check, one deployed environment)

**Depends on**: 4a-scaffold (done), 4c-dev-authentication (drafted, not yet approved). This spec does not
redefine sign-in, tokens or the login screen; those stay in 4c.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See a consistent app shell (Priority: P1)

A signed-in person opens the app and sees a common frame: a top bar with the product name, role-based
navigation links and a user menu showing who they are. The links shown depend on the person's roles. The
sections are placeholders for now, and each leads to an empty "coming soon" page.

**Why this priority**: Every later screen (apply leave, approvals, balances, admin) lives inside this frame.
Without it nothing else can be shown to a role-aware audience.

**Independent Test**: Sign in as an Employee, a Manager, an HR person and an Admin; each sees the same header
and a navigation list that differs by role.

**Acceptance Scenarios**:

1. **Given** a signed-in Employee, **When** the app loads, **Then** the top bar shows the app name and the person's name, and navigation lists only Dashboard and My requests.
2. **Given** a signed-in person who is also a Manager, **When** the app loads, **Then** navigation additionally lists Approvals (roles are additive). HR adds Reports, Exceptions and Audit; Admin adds Leave types, Holidays and Audit.
3. **Given** a person who picks a navigation item, **When** the page opens, **Then** the shell stays in place and the content area shows that section's placeholder page.
4. **Given** a person without a session, **When** they open the app, **Then** they do not see the shell; they are taken to sign-in (behavior defined in 4c).

---

### User Story 2 - See that the API is reachable (Priority: P1)

Inside the shell, a person (and the developer) can see whether the back end is up, and its status, environment
and sign-in mode, with a way to check again.

**Why this priority**: It is the proof that the front end and back end are connected, in every environment.

**Independent Test**: Start the app with the API running and then stopped; the shell shows "healthy" and then a clear "could not reach the API" message.

**Acceptance Scenarios**:

1. **Given** the API is running, **When** the shell loads, **Then** the health area shows status, environment and sign-in mode.
2. **Given** the API is not reachable, **When** the shell loads, **Then** a readable error appears with a "Check again" action, and the rest of the shell still works.
3. **Given** the health area shows an error, **When** the API comes back and the person chooses "Check again", **Then** it shows healthy without reloading the page.

---

### User Story 3 - Choose the sign-in mode by configuration (Priority: P2)

A developer runs the app in dev sign-in mode purely through configuration, with no external accounts, and the
shell reads the mode from the API rather than assuming it.

**Why this priority**: Needed for local work and the deployed demo, but it depends on 4c for the actual sign-in.

**Independent Test**: Change the configured mode and restart; the shell reports the mode it finds, and no code change is needed.

**Acceptance Scenarios**:

1. **Given** dev mode is configured in a development setting, **When** the app runs, **Then** dev sign-in is available and the health area reports "Dev".
2. **Given** dev mode is configured outside development, **When** the API starts, **Then** it refuses to start (rule from 4c, restated here so the deployed environment honors it).

---

### User Story 4 - Reach one deployed environment (Priority: P2)

The owner can open the app at a public address on a free-tier host, sign in with a demo person, and see the
shell and a healthy API. A cost alert warns the owner before any charge.

**Why this priority**: The sprint goal is a working, shareable environment for the portfolio.

**Independent Test**: Open the public address from a different device; the shell loads and the health area is healthy.

**Acceptance Scenarios**:

1. **Given** the environment is deployed, **When** the owner opens its address, **Then** the shell loads and shows a healthy API.
2. **Given** spend on the environment approaches the alert level, **When** it crosses it, **Then** the owner is notified.
3. **Given** the deployed environment, **When** it is inspected, **Then** no secret appears in the repository or the published web files.

---

### Edge Cases

- API down, slow or blocked by origin rules: the shell shows an error state, not a blank page.
- Token expires while the shell is open: handled by 4c (sign out and return to sign-in).
- A person has several roles: navigation shows the union of sections with no duplicates.
- A person has an unknown or empty role list: only Employee sections show (everyone is an Employee).
- Narrow (phone-width) screen: header and navigation remain usable without horizontal page scrolling.
- Direct visit to an unknown address: a safe "page not found" view (screen S-12) with a "Back to dashboard" link.
- Deep link to a section the person's roles do not include: the placeholder is not shown; the person goes to their home page. The server still enforces access when real data arrives.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST present a common shell (header, navigation, content area) to every signed-in person.
- **FR-002**: The header MUST show the product name and the signed-in person's display name.
- **FR-003**: Navigation MUST list placeholder sections per role: Employee for everyone, plus Manager, HR and Admin sections where the person holds that role.
- **FR-004**: Navigation visibility MUST be treated as a convenience only; the server remains the authority for every rule (constitution I).
- **FR-005**: The shell MUST show API health (status, environment, sign-in mode) and offer a "check again" action, with visible loading and error states.
- **FR-006**: The shell MUST be usable on desktop and phone widths, with labeled controls and keyboard access.
- **FR-007**: The sign-in mode MUST be selected by configuration only, and business code MUST NOT branch on it (security rule 5).
- **FR-008**: Dev mode MUST be refused outside the `Development` and `Demo` environments (security rule 6, amended 2026-11-02 so one restricted deployed demo can use dev sign-in).
- **FR-009**: One environment MUST be deployed to the Azure free tier and be reachable at a public address by the owner, using demo sign-in. (Pulls one environment forward from Phase 6.)
- **FR-010**: The deployed environment MUST have a cost alert that notifies the owner by email, using the host's free alerting. The threshold is set in the design step and confirmed by the owner.
- **FR-011**: The shell layout MUST follow the Phase 3a UI/UX design (Confluence LMA, "Phase 3a: UI/UX Design"): a top bar with the app name, role-based links and a user menu; the design tokens (primary deep blue, Roboto 16px, 8px spacing); breakpoints at 360, 768 and 1280 pixels; and WCAG 2.1 AA accessibility rules (contrast, visible focus, keyboard-only use, labels, status never shown by color alone).
- **FR-012**: No secret MUST appear in the repository, an image or the published web files (security rule 11).
- **FR-013**: Package versions and any preview-status features used MUST be checked against current documentation before adoption, and new packages need the owner's approval first (CLAUDE.md).

### Key Entities *(include if feature involves data)*

- **Signed-in person**: id, display name and roles, as read from the sign-in (4c). The shell only reads them.
- **Navigation section**: a named destination with the roles that may see it. Placeholder only; no data.
- **Health report**: status, environment, sign-in mode, database state and server time, as returned by the health check.
- **Environment**: a named deployment (local, deployed demo) with its address and its configured sign-in mode.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After sign-in, a person sees the shell with the correct navigation for their roles within 3 seconds on a normal connection.
- **SC-002**: For each of the four demo roles, 100% of the navigation sections shown match the role table in the product document.
- **SC-003**: With the API stopped, 100% of page loads show a readable error state rather than a blank or broken page.
- **SC-004**: The owner can open the deployed address from a second device and see a healthy status, with no manual steps beyond the demo sign-in.
- **SC-005**: A cost alert exists on the deployed environment and the owner can show it working (a test notification).
- **SC-006**: A scan of the repository and the published web files finds zero secrets.
- **SC-007**: The shell has no horizontal page scrolling at a 320-pixel width.

## Assumptions

- Sign-in, tokens, the login screen and the person's identity call are delivered by 4c; this feature consumes them and can show the shell with a stub person until 4c is approved and built.
- Angular Material is the design system chosen in Phase 3a and named in the ticket. Adding the package still needs the owner's approval at implementation (CLAUDE.md).
- Phase 3a is marked "Draft for approval" and its checklist items "Navigation per role agreed" and "Colour pairs checked in a contrast checker" are unticked. This spec follows the draft; any change at approval flows into this spec.
- Phase 3a has wireframes for 6 of 12 screens and none for the shell itself, so the exact top-bar layout is settled in the design step from the S-02 wireframe.
- The cost alert amount is not fixed yet; the design step proposes a low monthly threshold for the owner to confirm.
- Navigation items are placeholders; real screens arrive in 4d to 4f.
- Roles come from the sign-in claims; changeable facts such as the manager are never read from the token (security rule 7).
- One office location and English only, as in the MVP.
- The existing 4a health display is reused and moved into the shell rather than rebuilt.
- One environment means one deployed copy of the web app and API with its database; production-grade hardening (BFF cookies, rate limits) is out of scope.
- Out of scope: leave screens, real data in navigation pages, Entra ID, Redis, RabbitMQ, CI/CD beyond what the one deployment needs.
