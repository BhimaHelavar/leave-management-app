import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { Role } from './models';
import { roleGuard } from './role-guard';

function run(path: string | undefined, roles: readonly Role[]): boolean | UrlTree {
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: AuthService, useValue: { roles: signal(roles) } }],
  });
  const route = { routeConfig: path === undefined ? null : { path } } as ActivatedRouteSnapshot;
  return TestBed.runInInjectionContext(() =>
    roleGuard(route, {} as RouterStateSnapshot),
  ) as boolean | UrlTree;
}

describe('roleGuard', () => {
  it('allows a person who holds a role listed for the route', () => {
    expect(run('approvals', ['Employee', 'Manager'])).toBe(true);
  });

  it('sends a person without the role to the not-found view', () => {
    const result = run('approvals', ['Employee']);

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/not-found');
  });

  it('sends a person with no session to the not-found view', () => {
    const result = run('dashboard', []);

    expect(result).toBeInstanceOf(UrlTree);
  });

  it('blocks a route that is not in the navigation table', () => {
    expect(run('secret', ['Employee', 'Admin'])).toBeInstanceOf(UrlTree);
  });

  it('blocks a route that has no path', () => {
    expect(run(undefined, ['Employee', 'Admin'])).toBeInstanceOf(UrlTree);
  });
});
