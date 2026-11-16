import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { NEVER } from 'rxjs';
import { routes } from './app.routes';
import { LeaveRequestService } from './features/apply-leave/leave-request.service';
import { ApprovalsService } from './features/approvals/approvals.service';
import { BalanceService } from './features/dashboard/balance.service';
import { authGuard } from './auth/auth.guard';
import { AuthService } from './auth/auth.service';
import { Role } from './auth/models';
import { roleGuard } from './auth/role-guard';
import { NAV_ITEMS } from './layout/nav-items';

async function navigate(
  url: string,
  roles: readonly Role[],
  signedIn = true,
): Promise<{ url: string; text: string }> {
  TestBed.configureTestingModule({
    providers: [
      provideRouter(routes),
      // The dashboard route loads real data; these tests only check routing, so the balances load is stubbed.
      { provide: BalanceService, useValue: { getMyBalances: () => NEVER } },
      { provide: LeaveRequestService, useValue: { apply: () => NEVER } },
      {
        provide: ApprovalsService,
        useValue: { getApprovals: () => NEVER, getApproval: () => NEVER },
      },
      { provide: AuthService, useValue: { roles: signal(roles), isSignedIn: signal(signedIn) } },
    ],
  });
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  return {
    url: TestBed.inject(Router).url,
    text: (harness.routeNativeElement as HTMLElement).textContent ?? '',
  };
}

describe('routes', () => {
  it('has one guarded route per navigation item', () => {
    for (const item of NAV_ITEMS) {
      const route = routes.find((r) => r.path === item.path);
      expect(route?.canActivate).toEqual([authGuard, roleGuard]);
      expect(route?.data?.['title']).toBe(item.label);
    }
  });

  it('redirects the root to the dashboard', async () => {
    expect((await navigate('/', ['Employee'])).url).toBe('/dashboard');
  });

  it('opens a section the person may see and shows its placeholder', async () => {
    const result = await navigate('/reports', ['Employee', 'HR']);

    expect(result.url).toBe('/reports');
    expect(result.text).toContain('Reports');
    expect(result.text).toContain('Coming soon');
  });

  it('opens the manager inbox for a manager', async () => {
    const result = await navigate('/approvals', ['Employee', 'Manager']);

    expect(result.url).toBe('/approvals');
    expect(result.text).toContain('Requests waiting for you');
    expect(result.text).not.toContain('Coming soon');
  });

  it('opens a request page for a manager', async () => {
    const result = await navigate('/approvals/42', ['Employee', 'Manager']);

    expect(result.url).toBe('/approvals/42');
    expect(result.text).toContain('Request');
  });

  it('sends an employee who opens a request page to the not-found view', async () => {
    const result = await navigate('/approvals/42', ['Employee']);

    expect(result.url).toBe('/not-found');
  });

  it('sends a signed-out visit to a request page to sign-in with the wanted address', async () => {
    const result = await navigate('/approvals/42', [], false);

    expect(result.url).toBe('/login?returnUrl=%2Fapprovals%2F42');
  });

  it('opens the Apply for leave screen for a signed-in person', async () => {
    const result = await navigate('/apply-leave', ['Employee']);

    expect(result.url).toBe('/apply-leave');
    expect(result.text).toContain('Apply for leave');
    expect(result.text).not.toContain('Coming soon');
  });

  it('sends a signed-out person to the sign-in screen', async () => {
    const result = await navigate('/dashboard', [], false);

    expect(result.url).toBe('/login?returnUrl=%2Fdashboard');
  });

  it('sends a person without the role to the not-found view', async () => {
    const result = await navigate('/approvals', ['Employee']);

    expect(result.url).toBe('/not-found');
    expect(result.text).toContain('Page not found');
  });

  it('shows the not-found view for an unknown address', async () => {
    const result = await navigate('/nothing-here', ['Employee']);

    expect(result.text).toContain('Page not found');
  });
});
