# Contract: half-day choice on the Apply for leave screen

## API

**Unchanged.** `POST /api/me/leave-requests` and `GET /api/me/leave-requests/preview` keep the contract in
`ai/specs/us03-apply-leave-working-days/contracts/leave-requests-api.md`. The `halfDay` field stays `None`, `FirstHalf` or `SecondHalf`. The refusal `halfday.invalid` (HTTP 422) stays as is.

## Screen contract (what the person sees)

Control: select, label "Length of the day", element id `half-day`. Options: Full day (`None`), First half (`FirstHalf`), Second half (`SecondHalf`).

| Dates on the form | Control | Hint (id `half-day-hint`) | Value sent |
| --- | --- | --- | --- |
| Start or end empty | visible, disabled | shown | `None` |
| Start and end differ | visible, disabled | shown | `None` |
| Start equals end | visible, enabled | hidden | the chosen value |

Hint text while disabled: "A half day applies to a single day only. Choose the same start and end date to use it."

Rules:
- The control is never removed from the page.
- The select has `aria-describedby="half-day-hint"` while the hint is shown.
- The hint sits inside an always-present `aria-live="polite"` wrapper, so a change of dates is announced (review finding, added after T003).
- When the control becomes disabled its displayed value returns to "Full day".
- The preview and the submit send `halfDay: 'None'` whenever the control is disabled.

## Examples

| Input | Result |
| --- | --- |
| Start 2026-12-01, end 2026-12-01, pick First half | Preview 0.5. Submit sends `halfDay: 'FirstHalf'`. |
| Start 2026-12-01, end 2026-12-02 | Control disabled with hint. Submit sends `halfDay: 'None'`. |
| Pick First half on one day, then set end to 2026-12-02 | Control disabled and shows Full day. |
