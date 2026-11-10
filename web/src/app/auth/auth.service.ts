import { Injectable, computed, inject, signal } from '@angular/core';
import { RuntimeConfigService } from '../config/runtime-config';
import { Person, Role, normalizeRoles } from './models';

/** Local demo people for the shell until real sign-in (spec 4c) exists. Fake data, not real users. */
export const STUB_PEOPLE: readonly Person[] = [
  { id: 'stub-employee', name: 'Demo Employee', roles: normalizeRoles(['Employee']) },
  { id: 'stub-manager', name: 'Demo Manager', roles: normalizeRoles(['Employee', 'Manager']) },
  { id: 'stub-hr', name: 'Demo HR', roles: normalizeRoles(['Employee', 'HR']) },
  { id: 'stub-admin', name: 'Demo Admin', roles: normalizeRoles(['Employee', 'Admin']) },
];

/**
 * The one place the shell asks "who is signed in?". Spec 4c replaces the stub with the person
 * read from the API's token and `/api/me`, without changing callers.
 *
 * Hiding a link based on these roles is a convenience only; the server decides (security rule 1).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly runtimeConfig = inject(RuntimeConfigService);
  private readonly current = signal<Person | null>(null);

  readonly person = this.current.asReadonly();
  readonly roles = computed<readonly Role[]>(() => this.current()?.roles ?? []);

  /** The stub is on only when config.json says so; the deployed file never does. */
  readonly stubEnabled =
    this.runtimeConfig.state() === 'ready' && this.runtimeConfig.config.devStub;

  readonly stubPeople = this.stubEnabled ? STUB_PEOPLE : [];

  constructor() {
    if (this.stubEnabled) {
      this.current.set(STUB_PEOPLE[0]);
    }
  }

  /** Switches the stub person. Does nothing (returns false) unless the stub is enabled. */
  useStubPerson(id: string): boolean {
    const person = this.stubPeople.find((p) => p.id === id);
    if (!person) {
      return false;
    }
    this.current.set(person);
    return true;
  }
}
