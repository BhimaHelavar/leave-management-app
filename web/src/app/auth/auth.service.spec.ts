import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ConfigState, RuntimeConfigService } from '../config/runtime-config';
import { AuthService } from './auth.service';
import { normalizeRoles } from './models';

function createService(state: ConfigState, devStub: boolean): AuthService {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: RuntimeConfigService,
        useValue: { state: signal(state), config: { apiUrl: 'http://api.test', devStub } },
      },
    ],
  });
  return TestBed.inject(AuthService);
}

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
  it('starts as the stub Employee when the stub is enabled', () => {
    const auth = createService('ready', true);

    expect(auth.person()?.name).toBe('Demo Employee');
    expect(auth.roles()).toEqual(['Employee']);
  });

  it('gives the right roles for each of the four stub people', () => {
    const auth = createService('ready', true);
    const expected: Record<string, string[]> = {
      'stub-employee': ['Employee'],
      'stub-manager': ['Employee', 'Manager'],
      'stub-hr': ['Employee', 'HR'],
      'stub-admin': ['Employee', 'Admin'],
    };

    expect(auth.stubPeople.length).toBe(4);
    for (const [id, roles] of Object.entries(expected)) {
      expect(auth.useStubPerson(id)).toBe(true);
      expect([...auth.roles()]).toEqual(roles);
    }
  });

  it('ignores an unknown stub person id', () => {
    const auth = createService('ready', true);

    expect(auth.useStubPerson('nobody')).toBe(false);
    expect(auth.person()?.id).toBe('stub-employee');
  });

  it('has no person and no stub people when devStub is false', () => {
    const auth = createService('ready', false);

    expect(auth.person()).toBeNull();
    expect(auth.roles()).toEqual([]);
    expect(auth.stubPeople).toEqual([]);
    expect(auth.useStubPerson('stub-admin')).toBe(false);
    expect(auth.person()).toBeNull();
  });

  it('has no person when the configuration failed to load', () => {
    const auth = createService('failed', true);

    expect(auth.person()).toBeNull();
  });
});
