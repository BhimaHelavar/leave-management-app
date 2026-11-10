import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { Person, Role, normalizeRoles } from '../auth/models';
import { TopBar } from './top-bar';

@Component({ template: '' })
class Blank {}

const EMPLOYEE: Person = { id: '4', name: 'Asha Nair', roles: normalizeRoles(['Employee']) };
const ADMIN: Person = { id: '3', name: 'Arjun Das', roles: normalizeRoles(['Employee', 'Admin']) };

interface Setup {
  fixture: ComponentFixture<TopBar>;
  person: ReturnType<typeof signal<Person | null>>;
  signOut: ReturnType<typeof vi.fn>;
  navigate: ReturnType<typeof vi.spyOn>;
  root: HTMLElement;
  narrow: BehaviorSubject<{ matches: boolean }>;
}

function setup(options: { person?: Person | null; narrow?: boolean } = {}): Setup {
  const narrow = new BehaviorSubject({ matches: options.narrow ?? false });
  const person = signal<Person | null>(options.person === undefined ? EMPLOYEE : options.person);
  const signOut = vi.fn();
  const auth = {
    person,
    roles: () => (person()?.roles ?? []) as readonly Role[],
    signOut,
  };
  TestBed.configureTestingModule({
    providers: [
      provideRouter([{ path: 'dashboard', component: Blank }]),
      { provide: AuthService, useValue: auth },
      { provide: BreakpointObserver, useValue: { observe: () => narrow } },
    ],
  });
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
  const fixture = TestBed.createComponent(TopBar);
  fixture.detectChanges();
  return { fixture, person, signOut, navigate, root: fixture.nativeElement as HTMLElement, narrow };
}

const linkLabels = (root: HTMLElement) =>
  Array.from(root.querySelectorAll('nav a')).map((a) => a.textContent?.trim());

describe('TopBar', () => {
  it('shows the app name and the signed-in person, with Employee links only for an Employee', () => {
    const { root } = setup();

    expect(root.querySelector('.brand')?.textContent).toContain('Leave Management');
    expect(root.textContent).toContain('Asha Nair');
    expect(linkLabels(root)).toEqual(['Dashboard', 'My requests']);
  });

  it('shows the Admin links for an Admin', () => {
    const { root } = setup({ person: ADMIN });

    expect(linkLabels(root)).toEqual([
      'Dashboard',
      'My requests',
      'Leave types',
      'Holidays',
      'Audit',
    ]);
    expect(root.textContent).toContain('Arjun Das');
  });

  it('collapses the links into one labelled menu button at narrow width', () => {
    const { root } = setup({ narrow: true });

    expect(linkLabels(root)).toEqual([]);
    const button = root.querySelector('button[aria-label="Open navigation menu"]');
    expect(button).not.toBeNull();
  });

  it('switches between links and the menu button when the width changes', () => {
    const { fixture, root, narrow } = setup({ narrow: false });
    expect(root.querySelector('button[aria-label="Open navigation menu"]')).toBeNull();

    narrow.next({ matches: true });
    fixture.detectChanges();

    expect(root.querySelector('button[aria-label="Open navigation menu"]')).not.toBeNull();
  });

  it('labels the user menu with the person name', () => {
    const { root } = setup();

    expect(root.querySelector('button[aria-label="Account menu for Asha Nair"]')).not.toBeNull();
  });

  it('marks the current page with aria-current', async () => {
    const { fixture, root } = setup();

    await TestBed.inject(Router).navigateByUrl('/dashboard');
    fixture.detectChanges();

    const current = root.querySelector('nav a[aria-current="page"]');
    expect(current?.textContent?.trim()).toBe('Dashboard');
  });

  it('has the skip link as the first focusable element and moves focus to #main when used', () => {
    const { fixture, root } = setup();
    const main = document.createElement('main');
    main.id = 'main';
    main.tabIndex = -1;
    document.body.appendChild(main);

    const first = root.querySelector('a, button');
    expect(first?.classList.contains('skip-link')).toBe(true);

    (first as HTMLElement).click();
    fixture.detectChanges();

    expect(document.activeElement).toBe(main);
    main.remove();
  });

  it('shows only the app name when nobody is signed in', () => {
    const { root } = setup({ person: null });

    expect(root.querySelector('.brand')).not.toBeNull();
    expect(root.querySelector('nav')).toBeNull();
    expect(root.querySelector('button')).toBeNull();
  });

  it('signs out and goes to the sign-in screen from the user menu', async () => {
    const { fixture, signOut, navigate } = setup();
    navigate.mockResolvedValue(true);

    (
      fixture.nativeElement.querySelector('button[aria-label^="Account menu"]') as HTMLButtonElement
    ).click();
    fixture.detectChanges();
    await fixture.whenStable();
    const item = Array.from(document.querySelectorAll('[mat-menu-item]')).find(
      (el) => el.textContent?.trim() === 'Sign out',
    ) as HTMLElement;
    item.click();
    await fixture.whenStable();

    expect(signOut).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
