import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NEVER, Observable, Subject, of, throwError } from 'rxjs';
import { Approvals } from './approvals';
import { ApprovalItem, ApprovalsService, ManagerInbox } from './approvals.service';

function item(id: number, name: string, overrides: Partial<ApprovalItem> = {}): ApprovalItem {
  return {
    requestId: id,
    employee: { id: id + 100, name },
    leaveType: { id: 1, code: 'ANNUAL', name: 'Annual leave' },
    startDate: '2026-12-01',
    endDate: '2026-12-02',
    halfDay: 'None',
    daysCount: 2,
    submittedAtUtc: '2026-11-14T11:51:50Z',
    balance: { before: 8, after: 6 },
    teamOverlap: { teamSize: 3, awayIfApproved: 1, others: [] },
    ...overrides,
  };
}

function inbox(items: ApprovalItem[], page = 1, totalCount = items.length): ManagerInbox {
  return { items, page, pageSize: 20, totalCount };
}

type GetFn = (page: number, pageSize: number) => Observable<ManagerInbox>;

async function render(get: GetFn): Promise<{
  fixture: ComponentFixture<Approvals>;
  page: HTMLElement;
  getSpy: ReturnType<typeof vi.fn<GetFn>>;
}> {
  const getSpy = vi.fn<GetFn>(get);
  TestBed.configureTestingModule({
    providers: [{ provide: ApprovalsService, useValue: { getApprovals: getSpy } }],
  });
  const fixture = TestBed.createComponent(Approvals);
  await fixture.whenStable();
  return { fixture, page: fixture.nativeElement as HTMLElement, getSpy };
}

function button(page: HTMLElement, text: string): HTMLButtonElement {
  return Array.from(page.querySelectorAll('button')).find(
    (b) => b.textContent?.trim() === text,
  ) as HTMLButtonElement;
}

describe('Approvals', () => {
  it('shows the loading state while the requests load', async () => {
    const { page } = await render(() => NEVER);

    expect(page.querySelector('[role="status"]')?.textContent).toContain('Loading requests');
  });

  it('lists the requests in the order the server gave, with the fields', async () => {
    const { page, getSpy } = await render(() =>
      of(
        inbox([
          item(1, 'Asha Nair', { startDate: '2026-12-01', endDate: '2026-12-02' }),
          item(2, 'Ravi Kumar', {
            startDate: '2026-12-04',
            endDate: '2026-12-04',
            daysCount: 0.5,
            halfDay: 'FirstHalf',
            balance: { before: 6, after: 5.5 },
          }),
        ]),
      ),
    );

    expect(getSpy).toHaveBeenCalledWith(1, 20);
    const cards = Array.from(page.querySelectorAll('.request'));
    expect(cards).toHaveLength(2);
    expect(cards[0].textContent).toContain('Asha Nair');
    expect(cards[0].textContent).toContain('Annual leave');
    expect(cards[0].textContent).toContain('2026-12-01 to 2026-12-02');
    expect(cards[0].textContent).toContain('Before 8.0, after 6.0');
    expect(cards[1].textContent).toContain('Ravi Kumar');
    expect(cards[1].textContent).toContain('2026-12-04');
    expect(cards[1].textContent).not.toContain(' to 2026-12-04');
    expect(cards[1].textContent).toContain('First half');
    expect(cards[1].textContent).toContain('0.5');
  });

  it('shows no balance line for a type with no balance', async () => {
    const { page } = await render(() => of(inbox([item(1, 'Asha Nair', { balance: null })])));

    expect(page.querySelector('.request')?.textContent).not.toContain('Balance');
  });

  it('shows a clear message when nothing is waiting', async () => {
    const { page } = await render(() => of(inbox([])));

    expect(page.querySelector('.empty')?.textContent).toContain(
      'Nothing is waiting for your decision.',
    );
    expect(page.querySelector('.request')).toBeNull();
  });

  it('shows an error with a retry button, not an empty list, when the load fails', async () => {
    let calls = 0;
    const { fixture, page } = await render(() => {
      calls++;
      return calls === 1
        ? throwError(() => new HttpErrorResponse({ status: 500 }))
        : of(inbox([item(1, 'Asha Nair')]));
    });

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');
    expect(page.querySelector('.empty')).toBeNull();

    button(page, 'Try again').click();
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')).toBeNull();
    expect(page.querySelector('.request')?.textContent).toContain('Asha Nair');
  });

  describe('team overlap', () => {
    it('says no one else is away when there is no overlap', async () => {
      const { page } = await render(() => of(inbox([item(1, 'Asha Nair')])));

      expect(page.querySelector('.overlap')?.textContent).toContain(
        'No one else in your team is away on these days.',
      );
    });

    it('names the others with the shared dates and shows the figure the server gave', async () => {
      const { page } = await render(() =>
        of(
          inbox([
            item(1, 'Sneha Iyer', {
              teamOverlap: {
                teamSize: 3,
                awayIfApproved: 3,
                others: [
                  { employeeId: 104, name: 'Asha Nair', dates: ['2026-12-02'] },
                  { employeeId: 105, name: 'Ravi Kumar', dates: ['2026-12-01', '2026-12-02'] },
                ],
              },
            }),
          ]),
        ),
      );

      const text = page.querySelector('.overlap')?.textContent ?? '';
      expect(text).toContain(
        'Also away: Asha Nair (2026-12-02); Ravi Kumar (2026-12-01, 2026-12-02).',
      );
      expect(text).toContain('3 of 3 of your team would be away if you approve.');
    });

    it('shows the count from the data, not one it works out itself', async () => {
      const { page } = await render(() =>
        of(
          inbox([
            item(1, 'Sneha Iyer', {
              teamOverlap: {
                teamSize: 5,
                awayIfApproved: 2,
                others: [{ employeeId: 104, name: 'Asha Nair', dates: ['2026-12-02'] }],
              },
            }),
          ]),
        ),
      );

      expect(page.querySelector('.overlap')?.textContent).toContain(
        '2 of 5 of your team would be away if you approve.',
      );
    });
  });

  describe('paging', () => {
    it('asks for the next page and disables Previous on page 1', async () => {
      const { fixture, page, getSpy } = await render((p) =>
        of(inbox([item(p, `Person ${p}`)], p, 45)),
      );

      expect(page.querySelector('.paging')?.textContent).toContain('Page 1 of 3');
      expect(button(page, 'Previous').disabled).toBe(true);
      expect(button(page, 'Next').disabled).toBe(false);

      button(page, 'Next').click();
      await fixture.whenStable();

      expect(getSpy).toHaveBeenLastCalledWith(2, 20);
      expect(page.querySelector('.paging')?.textContent).toContain('Page 2 of 3');
      expect(button(page, 'Previous').disabled).toBe(false);
    });

    it('disables Next on the last page', async () => {
      const { page } = await render(() => of(inbox([item(1, 'Asha Nair')], 3, 45)));

      expect(page.querySelector('.paging')?.textContent).toContain('Page 3 of 3');
      expect(button(page, 'Next').disabled).toBe(true);
    });
  });

  describe('stale and overlapping loads', () => {
    type Loadable = { load(page?: number): void };

    it('reloads the last valid page when the page is empty but requests remain', async () => {
      const { fixture, page, getSpy } = await render(() =>
        of(inbox([item(1, 'Asha Nair')], 1, 45)),
      );
      // Page 5 existed a moment ago; now only 25 remain, so the server returns it empty.
      getSpy
        .mockReturnValueOnce(of(inbox([], 5, 25)))
        .mockReturnValueOnce(of(inbox([item(2, 'Last Page')], 2, 25)));

      (fixture.componentInstance as unknown as Loadable).load(5);
      await fixture.whenStable();

      expect(getSpy).toHaveBeenNthCalledWith(2, 5, 20);
      expect(getSpy).toHaveBeenNthCalledWith(3, 2, 20);
      expect(page.querySelector('.empty')).toBeNull();
      expect(page.querySelector('.request')?.textContent).toContain('Last Page');
      expect(page.querySelector('.paging')?.textContent).toContain('Page 2 of 2');
    });

    it('ignores the late response of an older request', async () => {
      const { fixture, page, getSpy } = await render(() =>
        of(inbox([item(1, 'Asha Nair')], 1, 45)),
      );
      const older = new Subject<ManagerInbox>();
      const newer = new Subject<ManagerInbox>();
      getSpy.mockReturnValueOnce(older).mockReturnValueOnce(newer);
      const component = fixture.componentInstance as unknown as Loadable;

      component.load(2);
      component.load(3);
      newer.next(inbox([item(3, 'Newest')], 3, 45));
      older.next(inbox([item(2, 'Stale')], 2, 45));
      await fixture.whenStable();

      expect(older.observed).toBe(false);
      expect(page.querySelector('.request')?.textContent).toContain('Newest');
      expect(page.textContent).not.toContain('Stale');
      expect(page.querySelector('.paging')?.textContent).toContain('Page 3 of 3');
    });
  });

  it('has no Approve or Reject button yet (deciding is the next story)', async () => {
    const { page } = await render(() => of(inbox([item(1, 'Asha Nair')])));

    const labels = Array.from(page.querySelectorAll('button')).map((b) => b.textContent?.trim());
    expect(labels).not.toContain('Approve');
    expect(labels).not.toContain('Reject');
  });
});
