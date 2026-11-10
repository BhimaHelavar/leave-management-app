import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { RuntimeConfigService } from '../config/runtime-config';
import { TopBar } from './top-bar';

@Component({ template: '' })
class Blank {}

interface Setup {
  fixture: ComponentFixture<TopBar>;
  auth: AuthService;
  root: HTMLElement;
  narrow: BehaviorSubject<{ matches: boolean }>;
}

function setup(options: { devStub?: boolean; narrow?: boolean } = {}): Setup {
  const narrow = new BehaviorSubject({ matches: options.narrow ?? false });
  TestBed.configureTestingModule({
    providers: [
      provideRouter([{ path: 'dashboard', component: Blank }]),
      {
        provide: RuntimeConfigService,
        useValue: {
          state: signal('ready'),
          config: { apiUrl: 'http://api.test', devStub: options.devStub ?? true },
        },
      },
      { provide: BreakpointObserver, useValue: { observe: () => narrow } },
    ],
  });
  const auth = TestBed.inject(AuthService);
  const fixture = TestBed.createComponent(TopBar);
  fixture.detectChanges();
  return { fixture, auth, root: fixture.nativeElement as HTMLElement, narrow };
}

const linkLabels = (root: HTMLElement) =>
  Array.from(root.querySelectorAll('nav a')).map((a) => a.textContent?.trim());

describe('TopBar', () => {
  it('shows the app name and the signed-in person, with Employee links only for an Employee', () => {
    const { root } = setup();

    expect(root.querySelector('.brand')?.textContent).toContain('Leave Management');
    expect(root.textContent).toContain('Demo Employee');
    expect(linkLabels(root)).toEqual(['Dashboard', 'My requests']);
  });

  it('shows the Admin links after switching to the Admin person', async () => {
    const { fixture, auth, root } = setup();

    auth.useStubPerson('stub-admin');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(linkLabels(root)).toEqual(['Dashboard', 'My requests', 'Leave types', 'Holidays', 'Audit']);
    expect(root.textContent).toContain('Demo Admin');
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

    expect(root.querySelector('button[aria-label="Account menu for Demo Employee"]')).not.toBeNull();
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
    const { root } = setup({ devStub: false });

    expect(root.querySelector('.brand')).not.toBeNull();
    expect(root.querySelector('nav')).toBeNull();
    expect(root.querySelector('button')).toBeNull();
  });
});
