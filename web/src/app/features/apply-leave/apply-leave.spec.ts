import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NEVER, Observable, Subject, of, throwError } from 'rxjs';
import { BalanceService, LeaveTypeBalance, MyBalances } from '../dashboard/balance.service';
import { ApplyLeave } from './apply-leave';
import {
  LeavePreview,
  LeaveRequestSaved,
  LeaveRequestService,
  PreviewQuery,
} from './leave-request.service';

const annual: LeaveTypeBalance = {
  leaveTypeId: 1,
  code: 'ANNUAL',
  name: 'Annual leave',
  hasBalance: true,
  accrued: 12,
  used: 0,
  pending: 0,
  available: 12,
  carryForwardDays: null,
  carryForwardExpiresOn: null,
};

const saved: LeaveRequestSaved = {
  id: 5,
  leaveTypeId: 1,
  startDate: '2026-11-27',
  endDate: '2026-12-01',
  halfDay: 'None',
  daysCount: 2,
  status: 'Pending',
  leaveYear: 2026,
};

type ApplyFn = (body: unknown, key: string) => Observable<LeaveRequestSaved>;
type PreviewFn = (query: PreviewQuery) => Observable<LeavePreview>;

async function render(
  apply: ApplyFn,
  balances: Observable<MyBalances> = of({ leaveYear: 2026, balances: [annual] }),
  preview: PreviewFn = () => NEVER,
) {
  const applySpy = vi.fn(apply);
  const previewSpy = vi.fn(preview);
  TestBed.configureTestingModule({
    providers: [
      { provide: BalanceService, useValue: { getMyBalances: () => balances } },
      { provide: LeaveRequestService, useValue: { apply: applySpy, preview: previewSpy } },
    ],
  });
  const fixture = TestBed.createComponent(ApplyLeave);
  await fixture.whenStable();
  return { fixture, page: fixture.nativeElement as HTMLElement, applySpy, previewSpy };
}

async function change(
  fixture: ComponentFixture<ApplyLeave>,
  selector: string,
  value: string,
  eventName: 'input' | 'change',
) {
  const field = (fixture.nativeElement as HTMLElement).querySelector(selector) as
    HTMLInputElement | HTMLSelectElement;
  field.value = value;
  field.dispatchEvent(new Event(eventName));
  await fixture.whenStable();
}

async function fillAndSubmit(fixture: ComponentFixture<ApplyLeave>) {
  await change(fixture, '#leave-type', '1', 'change');
  await change(fixture, '#start-date', '2026-11-27', 'input');
  await change(fixture, '#end-date', '2026-12-01', 'input');
  const form = (fixture.nativeElement as HTMLElement).querySelector('form') as HTMLFormElement;
  form.dispatchEvent(new Event('submit'));
  await fixture.whenStable();
}

describe('ApplyLeave', () => {
  it('saves the request and shows the day count the server returned', async () => {
    const { fixture, page, applySpy } = await render(() => of(saved));

    await fillAndSubmit(fixture);

    expect(applySpy).toHaveBeenCalledTimes(1);
    const [body, key] = applySpy.mock.calls[0];
    expect(body).toEqual({
      leaveTypeId: 1,
      startDate: '2026-11-27',
      endDate: '2026-12-01',
      halfDay: 'None',
      reason: null,
    });
    expect(key).toMatch(/^[A-Za-z0-9_-]{1,64}$/);
    expect(page.querySelector('.saved')?.textContent).toContain('Pending');
    expect(page.querySelector('.saved')?.textContent).toContain('2 days');
  });

  it('shows the message the server gave when the request is refused', async () => {
    const refusal = new HttpErrorResponse({
      status: 422,
      error: {
        title: 'Request refused',
        detail: 'You do not have enough balance for these days.',
        code: 'balance.insufficient',
      },
    });
    const { fixture, page } = await render(() => throwError(() => refusal));

    await fillAndSubmit(fixture);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'You do not have enough balance for these days.',
    );
    expect(page.querySelector('.saved')).toBeNull();
  });

  it.each([
    ['notice.short', "This leave type needs at least 3 working days' notice."],
    [
      'leave.overlap',
      'You already have a pending or approved leave request on one or more of these days.',
    ],
    ['balance.insufficient', 'You do not have enough balance for these days.'],
  ])('shows the refusal for %s in an alert', async (code, detail) => {
    const refusal = new HttpErrorResponse({
      status: code === 'leave.overlap' ? 409 : 422,
      error: { title: 'Request refused', detail, code },
    });
    const { fixture, page } = await render(() => throwError(() => refusal));

    await fillAndSubmit(fixture);

    const alert = page.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain(detail);
    expect(page.querySelector('.saved')).toBeNull();
  });

  it('falls back to the screen wording when a known code arrives without a detail', async () => {
    const refusal = new HttpErrorResponse({ status: 422, error: { code: 'year.cross' } });
    const { fixture, page } = await render(() => throwError(() => refusal));

    await fillAndSubmit(fixture);

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'Submit one request for each year.',
    );
  });

  it('shows the server title for a code the screen does not know', async () => {
    const refusal = new HttpErrorResponse({
      status: 422,
      error: { title: 'Request refused', detail: 'Some new rule text', code: 'brand.new' },
    });
    const { fixture, page } = await render(() => throwError(() => refusal));

    await fillAndSubmit(fixture);

    const text = page.querySelector('[role="alert"]')?.textContent ?? '';
    expect(text).toContain('Request refused');
    expect(text).not.toContain('Some new rule text');
  });

  it('disables the button while the request is being sent', async () => {
    const pending = new Subject<LeaveRequestSaved>();
    const { fixture, page } = await render(() => pending);

    await fillAndSubmit(fixture);

    const button = page.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);

    pending.next(saved);
    pending.complete();
    await fixture.whenStable();
    expect(page.querySelector('.saved')).not.toBeNull();
  });

  it('does not send until a type and both dates are chosen', async () => {
    const { fixture, page, applySpy } = await render(() => of(saved));

    const button = page.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    (page.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(applySpy).not.toHaveBeenCalled();
  });

  describe('half-day choice', () => {
    const halfDayField = (page: HTMLElement) =>
      page.querySelector('#half-day') as HTMLSelectElement;

    it('is visible but disabled, with a hint, before any date is chosen', async () => {
      const { page } = await render(() => of(saved));

      expect(halfDayField(page)).not.toBeNull();
      expect(halfDayField(page).disabled).toBe(true);
      expect(page.querySelector('#half-day-hint')).not.toBeNull();
    });

    it('is visible but disabled and explained when start and end are different days', async () => {
      const { fixture, page } = await render(() => of(saved));

      await change(fixture, '#start-date', '2026-12-01', 'input');
      await change(fixture, '#end-date', '2026-12-02', 'input');

      expect(halfDayField(page)).not.toBeNull();
      expect(halfDayField(page).disabled).toBe(true);
      expect(halfDayField(page).getAttribute('aria-describedby')).toBe('half-day-hint');
      expect(page.querySelector('#half-day-hint')?.textContent).toContain('single day only');
      // The hint sits in a live region so a change of dates is announced.
      expect(page.querySelector('#half-day-hint')?.parentElement?.getAttribute('aria-live')).toBe(
        'polite',
      );
    });

    it('is enabled, without the hint, when start and end are the same day', async () => {
      const { fixture, page } = await render(() => of(saved));

      await change(fixture, '#start-date', '2026-12-01', 'input');
      await change(fixture, '#end-date', '2026-12-01', 'input');

      expect(halfDayField(page).disabled).toBe(false);
      expect(halfDayField(page).hasAttribute('aria-describedby')).toBe(false);
      expect(page.querySelector('#half-day-hint')).toBeNull();
    });

    it('goes back to full day and is disabled when the end date moves to a later day', async () => {
      const { fixture, page } = await render(() => of(saved));
      await change(fixture, '#start-date', '2026-12-01', 'input');
      await change(fixture, '#end-date', '2026-12-01', 'input');
      await change(fixture, '#half-day', 'FirstHalf', 'change');
      expect(halfDayField(page).value).toBe('FirstHalf');

      await change(fixture, '#end-date', '2026-12-02', 'input');

      expect(halfDayField(page).disabled).toBe(true);
      expect(halfDayField(page).value).toBe('None');
    });

    it('is enabled again, starting at full day, when the dates become one day again', async () => {
      const { fixture, page } = await render(() => of(saved));
      await change(fixture, '#start-date', '2026-12-01', 'input');
      await change(fixture, '#end-date', '2026-12-01', 'input');
      await change(fixture, '#half-day', 'SecondHalf', 'change');
      await change(fixture, '#end-date', '2026-12-02', 'input');

      await change(fixture, '#end-date', '2026-12-01', 'input');

      expect(halfDayField(page).disabled).toBe(false);
      expect(halfDayField(page).value).toBe('None');
    });

    // The server refusing a half day on a range (FR-006) is checked by the API calls in quickstart.md; this
    // test only covers what the screen sends and how it shows a refusal.
    it('sends a full-day request for a range and shows the refusal message it gets back', async () => {
      const refusal = new HttpErrorResponse({
        status: 422,
        error: { title: 'Request refused', code: 'halfday.invalid' },
      });
      const { fixture, page, applySpy } = await render(() => throwError(() => refusal));

      await fillAndSubmit(fixture);

      expect(halfDayField(page).disabled).toBe(true);
      const [body] = applySpy.mock.calls[0];
      expect((body as { halfDay: string }).halfDay).toBe('None');
      expect(page.querySelector('[role="alert"]')?.textContent).toContain(
        'A half day must be a single working day',
      );
    });
  });

  describe('a single half day', () => {
    const halfDayPreview: LeavePreview = {
      daysCount: 0.5,
      workingDays: ['2026-12-01'],
      skippedHolidays: [],
    };

    async function chooseSingleDay(fixture: ComponentFixture<ApplyLeave>, half: string) {
      await change(fixture, '#leave-type', '1', 'change');
      await change(fixture, '#start-date', '2026-12-01', 'input');
      await change(fixture, '#end-date', '2026-12-01', 'input');
      await change(fixture, '#half-day', half, 'change');
    }

    function submit(fixture: ComponentFixture<ApplyLeave>) {
      const form = (fixture.nativeElement as HTMLElement).querySelector('form') as HTMLFormElement;
      form.dispatchEvent(new Event('submit'));
      return fixture.whenStable();
    }

    it.each(['FirstHalf', 'SecondHalf'])(
      'asks for the %s count and shows the 0.5 the server gives',
      async (half) => {
        const { fixture, page, previewSpy } = await render(
          () => of(saved),
          undefined,
          () => of(halfDayPreview),
        );

        await chooseSingleDay(fixture, half);

        expect(previewSpy).toHaveBeenLastCalledWith({
          leaveTypeId: 1,
          startDate: '2026-12-01',
          endDate: '2026-12-01',
          halfDay: half,
        });
        expect(page.querySelector('.preview .count')?.textContent).toContain('0.5');
      },
    );

    it.each(['FirstHalf', 'SecondHalf'])('sends %s for one day on submit', async (half) => {
      const { fixture, page, applySpy } = await render(
        () => of({ ...saved, halfDay: half as 'FirstHalf' | 'SecondHalf', daysCount: 0.5 }),
        undefined,
        () => of(halfDayPreview),
      );

      await chooseSingleDay(fixture, half);
      await submit(fixture);

      const [body] = applySpy.mock.calls[0];
      expect(body).toEqual({
        leaveTypeId: 1,
        startDate: '2026-12-01',
        endDate: '2026-12-01',
        halfDay: half,
        reason: null,
      });
      expect(page.querySelector('.saved')?.textContent).toContain('0.5 days');
    });

    it('shows the refusal when the server does not allow a half day for the leave type', async () => {
      const refusal = new HttpErrorResponse({
        status: 422,
        error: { title: 'Request refused', code: 'halfday.invalid' },
      });
      const { fixture, page } = await render(
        () => throwError(() => refusal),
        undefined,
        () => of(halfDayPreview),
      );

      await chooseSingleDay(fixture, 'FirstHalf');
      await submit(fixture);

      expect(page.querySelector('[role="alert"]')?.textContent).toContain(
        'this leave type must allow half days',
      );
      expect(page.querySelector('.saved')).toBeNull();
    });
  });

  describe('preview', () => {
    const twoDays: LeavePreview = {
      daysCount: 2,
      workingDays: ['2026-11-27', '2026-12-01'],
      skippedHolidays: ['2026-11-30'],
    };

    it('shows the count the server gives once a type and both dates are chosen', async () => {
      const { fixture, page, previewSpy } = await render(
        () => of(saved),
        undefined,
        () => of(twoDays),
      );

      await change(fixture, '#leave-type', '1', 'change');
      await change(fixture, '#start-date', '2026-11-27', 'input');
      expect(previewSpy).not.toHaveBeenCalled();
      expect(page.querySelector('.preview .count')).toBeNull();

      await change(fixture, '#end-date', '2026-12-01', 'input');

      expect(previewSpy).toHaveBeenLastCalledWith({
        leaveTypeId: 1,
        startDate: '2026-11-27',
        endDate: '2026-12-01',
        halfDay: 'None',
      });
      expect(page.querySelector('.preview .count')?.textContent).toContain('2');
      expect(page.querySelector('.preview .dates')?.textContent).toContain('2026-12-01');
      expect(page.querySelector('.preview .skipped')?.textContent).toContain('2026-11-30');
    });

    it('changes the count when a date changes', async () => {
      const { fixture, page } = await render(
        () => of(saved),
        undefined,
        (query) =>
          of({
            daysCount: query.endDate === '2026-12-01' ? 2 : 3,
            workingDays: [],
            skippedHolidays: [],
          }),
      );

      await change(fixture, '#leave-type', '1', 'change');
      await change(fixture, '#start-date', '2026-11-27', 'input');
      await change(fixture, '#end-date', '2026-12-01', 'input');
      expect(page.querySelector('.preview .count')?.textContent).toContain('2');

      await change(fixture, '#end-date', '2026-12-02', 'input');
      expect(page.querySelector('.preview .count')?.textContent).toContain('3');
    });

    it('shows the server message when the dates break a rule, and no count', async () => {
      const refusal = new HttpErrorResponse({
        status: 422,
        error: {
          title: 'Request refused',
          detail: 'The start date cannot be in the past.',
          code: 'date.past',
        },
      });
      const { fixture, page } = await render(
        () => of(saved),
        undefined,
        () => throwError(() => refusal),
      );

      await change(fixture, '#leave-type', '1', 'change');
      await change(fixture, '#start-date', '2020-01-06', 'input');
      await change(fixture, '#end-date', '2020-01-07', 'input');

      expect(page.querySelector('.preview .note')?.textContent).toContain(
        'The start date cannot be in the past.',
      );
      expect(page.querySelector('.preview .count')).toBeNull();
    });

    it('ignores a late answer for dates that are no longer on screen', async () => {
      const slow = new Subject<LeavePreview>();
      const { fixture, page } = await render(
        () => of(saved),
        undefined,
        (query) => (query.endDate === '2026-12-01' ? slow : of({ ...twoDays, daysCount: 3 })),
      );

      await change(fixture, '#leave-type', '1', 'change');
      await change(fixture, '#start-date', '2026-11-27', 'input');
      await change(fixture, '#end-date', '2026-12-01', 'input'); // slow reply, still pending
      await change(fixture, '#end-date', '2026-12-02', 'input'); // newer dates, answered at once
      expect(page.querySelector('.preview .count')?.textContent).toContain('3');

      slow.next({ ...twoDays, daysCount: 2 }); // the old answer arrives late
      await fixture.whenStable();

      expect(page.querySelector('.preview .count')?.textContent).toContain('3');
    });

    it('does not change what is sent on submit: the server recounts', async () => {
      const { fixture, applySpy } = await render(
        () => of(saved),
        undefined,
        () => of(twoDays),
      );

      await fillAndSubmit(fixture);

      const [body] = applySpy.mock.calls[0];
      expect(body).not.toHaveProperty('daysCount');
    });
  });

  it('shows an error and a retry button when the leave types cannot be loaded', async () => {
    const { page } = await render(
      () => of(saved),
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'Leave types could not be loaded.',
    );
    expect(page.querySelector('form')).toBeNull();
  });
});
