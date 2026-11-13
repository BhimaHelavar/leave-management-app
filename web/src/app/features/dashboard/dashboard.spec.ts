import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { BalanceService, LeaveTypeBalance, MyBalances } from './balance.service';
import { Dashboard } from './dashboard';

const annual: LeaveTypeBalance = {
  leaveTypeId: 1,
  code: 'ANNUAL',
  name: 'Annual leave',
  hasBalance: true,
  accrued: 10,
  used: 2,
  pending: 3,
  available: 5,
  carryForwardDays: null,
  carryForwardExpiresOn: null,
};

const unpaid: LeaveTypeBalance = {
  leaveTypeId: 5,
  code: 'UNPAID',
  name: 'Unpaid leave',
  hasBalance: false,
  accrued: null,
  used: null,
  pending: null,
  available: null,
  carryForwardDays: null,
  carryForwardExpiresOn: null,
};

async function render(result: Observable<MyBalances>): Promise<HTMLElement> {
  TestBed.configureTestingModule({
    providers: [{ provide: BalanceService, useValue: { getMyBalances: () => result } }],
  });
  const fixture = TestBed.createComponent(Dashboard);
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

describe('Dashboard', () => {
  it('shows available, accrued, used and pending for each leave type', async () => {
    const page = await render(of({ leaveYear: 2026, balances: [annual] }));
    const text = page.textContent ?? '';

    expect(text).toContain('Annual leave');
    expect(text).toContain('Leave year 2026');
    expect(page.querySelector('.available .number')?.textContent).toBe('5');
    expect(text).toContain('Accrued');
    expect(page.querySelectorAll('dd').length).toBe(3);
    expect(Array.from(page.querySelectorAll('dd')).map((e) => e.textContent)).toEqual([
      '10',
      '2',
      '3',
    ]);
  });

  it('shows how many days are carry-forward and when they expire', async () => {
    const page = await render(
      of({
        leaveYear: 2026,
        balances: [{ ...annual, carryForwardDays: 3, carryForwardExpiresOn: '2027-03-31' }],
      }),
    );

    expect(page.querySelector('.carry-forward')?.textContent).toContain('3');
    expect(page.querySelector('.carry-forward')?.textContent).toContain('2027-03-31');
  });

  it('shows no carry-forward line when there is none', async () => {
    const page = await render(of({ leaveYear: 2026, balances: [annual] }));

    expect(page.querySelector('.carry-forward')).toBeNull();
  });

  it('shows a card without numbers for a leave type that has no balance', async () => {
    const page = await render(of({ leaveYear: 2026, balances: [unpaid] }));

    expect(page.textContent).toContain('No balance, unpaid');
    expect(page.querySelector('.available')).toBeNull();
  });

  it('shows an error and no figures when the load fails', async () => {
    const page = await render(throwError(() => new Error('boom')));

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');
    expect(page.querySelector('.card')).toBeNull();
  });
});
