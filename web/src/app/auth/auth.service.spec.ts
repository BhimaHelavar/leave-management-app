import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../api-config';
import { AuthService } from './auth.service';
import { TokenResponse, normalizeRoles } from './models';

const API = 'http://api.test';

function setup() {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: API_URL, useValue: API },
    ],
  });
  return { auth: TestBed.inject(AuthService), http: TestBed.inject(HttpTestingController) };
}

const tokenResponse = (roles: string[]): TokenResponse => ({
  accessToken: 'jwt-value',
  expiresAtUtc: '2026-11-10T12:00:00Z',
  user: { id: 3, name: 'Arjun Das', email: 'arjun.das@example.test', roles },
});

describe('normalizeRoles', () => {
  it('always includes Employee and keeps role order stable', () => {
    expect(normalizeRoles(['Admin', 'Manager'])).toEqual(['Employee', 'Manager', 'Admin']);
  });

  it('turns an empty list into Employee only', () => {
    expect(normalizeRoles([])).toEqual(['Employee']);
  });

  it('drops unknown roles', () => {
    expect(normalizeRoles(['Superuser', 'HR'])).toEqual(['Employee', 'HR']);
  });
});

describe('AuthService', () => {
  it('starts signed out with no person, roles or token', () => {
    const { auth } = setup();

    expect(auth.isSignedIn()).toBe(false);
    expect(auth.person()).toBeNull();
    expect(auth.roles()).toEqual([]);
    expect(auth.accessToken()).toBeNull();
  });

  it('loads the demo people from the API', async () => {
    const { auth, http } = setup();

    const promise = auth.devUsers();
    http.expectOne(`${API}/api/dev/users`).flush([
      {
        id: 1,
        fullName: 'Meera Rao',
        email: 'meera.rao@example.test',
        roles: ['Employee', 'Manager'],
      },
    ]);

    expect((await promise).map((p) => p.fullName)).toEqual(['Meera Rao']);
  });

  it('signs in: posts the id, keeps the token and exposes the person and roles', async () => {
    const { auth, http } = setup();

    const promise = auth.signInAs(3);
    const request = http.expectOne(`${API}/api/dev/token`);
    expect(request.request.body).toEqual({ employeeId: 3 });
    request.flush(tokenResponse(['Employee', 'Admin']));

    expect(await promise).toBe(true);
    expect(auth.isSignedIn()).toBe(true);
    expect(auth.person()).toEqual({ id: '3', name: 'Arjun Das', roles: ['Employee', 'Admin'] });
    expect(auth.accessToken()).toBe('jwt-value');
  });

  it('stays signed out when the API refuses the person', async () => {
    const { auth, http } = setup();

    const promise = auth.signInAs(99999);
    http
      .expectOne(`${API}/api/dev/token`)
      .flush({ title: 'Person not found' }, { status: 404, statusText: 'Not Found' });

    expect(await promise).toBe(false);
    expect(auth.isSignedIn()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('never writes the token to browser storage', async () => {
    const { auth, http } = setup();
    const local = vi.spyOn(Storage.prototype, 'setItem');

    const promise = auth.signInAs(3);
    http.expectOne(`${API}/api/dev/token`).flush(tokenResponse(['Employee']));
    await promise;

    expect(local).not.toHaveBeenCalled();
    local.mockRestore();
  });

  it('signs out: clears the person and the token', async () => {
    const { auth, http } = setup();
    const promise = auth.signInAs(3);
    http.expectOne(`${API}/api/dev/token`).flush(tokenResponse(['Employee']));
    await promise;

    auth.signOut();

    expect(auth.isSignedIn()).toBe(false);
    expect(auth.accessToken()).toBeNull();
    expect(auth.roles()).toEqual([]);
  });
});
