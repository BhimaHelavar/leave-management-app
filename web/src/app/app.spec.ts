import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { ConfigState, RuntimeConfigService } from './config/runtime-config';

function setup(state: ConfigState) {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [
      provideRouter(routes),
      {
        provide: RuntimeConfigService,
        useValue: { state: signal(state), config: { apiUrl: 'http://api.test', devStub: true } },
      },
    ],
  });
  const fixture = TestBed.createComponent(App);
  fixture.detectChanges();
  return { fixture, root: fixture.nativeElement as HTMLElement };
}

describe('App', () => {
  it('renders the top bar and a focusable main area for the page', () => {
    const { root } = setup('ready');

    expect(root.querySelector('app-top-bar')).not.toBeNull();
    const main = root.querySelector('main');
    expect(main?.id).toBe('main');
    expect(main?.getAttribute('tabindex')).toBe('-1');
    expect(main?.querySelector('router-outlet')).not.toBeNull();
  });

  it('shows the page for the current route inside the shell', async () => {
    const { fixture, root } = setup('ready');

    await TestBed.inject(Router).navigateByUrl('/requests');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(root.querySelector('app-top-bar')).not.toBeNull();
    expect(root.querySelector('main')?.textContent).toContain('My requests');
    expect(root.querySelector('main')?.textContent).toContain('Coming soon');
  });

  it('shows the not-found page inside the shell for a section the person may not open', async () => {
    const { fixture, root } = setup('ready');

    await TestBed.inject(Router).navigateByUrl('/approvals');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(root.querySelector('main')?.textContent).toContain('Page not found');
  });

  it('shows the configuration error and no shell when the configuration failed to load', () => {
    const { root } = setup('failed');

    expect(root.textContent).toContain('configuration could not be loaded');
    expect(root.querySelector('app-top-bar')).toBeNull();
    // The error page has its own <main>; the shell's #main (with the router outlet) is absent.
    expect(root.querySelector('#main')).toBeNull();
    expect(root.querySelector('router-outlet')).toBeNull();
  });
});
