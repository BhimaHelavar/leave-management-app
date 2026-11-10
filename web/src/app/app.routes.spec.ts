import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
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
    const result = await navigate('/approvals', ['Employee', 'Manager']);

    expect(result.url).toBe('/approvals');
    expect(result.text).toContain('Approvals');
    expect(result.text).toContain('Coming soon');
  });

  it('sends a signed-out person to the sign-in screen', async () => {
    const result = await navigate('/dashboard', [], false);

    expect(result.url).toBe('/login');
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
