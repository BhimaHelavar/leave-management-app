import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ConfigState, RuntimeConfigService } from '../config/runtime-config';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

const API = 'http://api.test';

function setup(token: string | null, configState: ConfigState = 'ready') {
  const auth = { accessToken: () => token, signOut: vi.fn(), isSignedIn: signal(token !== null) };
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      {
        provide: RuntimeConfigService,
        useValue: {
          state: signal(configState),
          // Like the real service: reading the config before it has loaded throws.
          get config() {
            if (configState !== 'ready') {
              throw new Error('Runtime configuration has not been loaded.');
            }
            return { apiUrl: API, devStub: false };
          },
        },
      },
      { provide: AuthService, useValue: auth },
    ],
  });
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  return {
    auth,
    navigate,
    http: TestBed.inject(HttpClient),
    ctrl: TestBed.inject(HttpTestingController),
  };
}

describe('authInterceptor', () => {
  it('lets the config.json request through while the configuration is still loading', () => {
    const { http, ctrl } = setup('jwt-value', 'loading');

    http.get('config.json').subscribe();

    const request = ctrl.expectOne('config.json').request;
    expect(request.headers.has('Authorization')).toBe(false);
  });

  it('adds the bearer token to calls to our API', () => {
    const { http, ctrl } = setup('jwt-value');

    http.get(`${API}/api/me`).subscribe();

    expect(ctrl.expectOne(`${API}/api/me`).request.headers.get('Authorization')).toBe(
      'Bearer jwt-value',
    );
  });

  it('does not add the token to other sites', () => {
    const { http, ctrl } = setup('jwt-value');

    http.get('http://other.test/data').subscribe();

    expect(ctrl.expectOne('http://other.test/data').request.headers.has('Authorization')).toBe(
      false,
    );
  });

  it('adds no header when nobody is signed in', () => {
    const { http, ctrl } = setup(null);

    http.get(`${API}/api/dev/users`).subscribe();

    expect(ctrl.expectOne(`${API}/api/dev/users`).request.headers.has('Authorization')).toBe(false);
  });

  it('signs out and goes to /login when the API answers 401', () => {
    const { http, ctrl, auth, navigate } = setup('jwt-value');

    http.get(`${API}/api/me`).subscribe({ error: () => undefined });
    ctrl.expectOne(`${API}/api/me`).flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(auth.signOut).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('leaves the session alone for other errors such as 500', () => {
    const { http, ctrl, auth, navigate } = setup('jwt-value');

    http.get(`${API}/api/me`).subscribe({ error: () => undefined });
    ctrl.expectOne(`${API}/api/me`).flush({}, { status: 500, statusText: 'Server Error' });

    expect(auth.signOut).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });
});
